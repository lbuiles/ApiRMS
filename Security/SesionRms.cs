using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace RmsErp.Api.Security;

/// <summary>
/// Sesión propia de RMS. El token de Google (dura ~1 hora y no se renueva) solo se usa una vez,
/// para iniciar sesión en POST /auth/sesion; desde ahí la app usa este token de 12 horas,
/// que se renueva solo mientras el usuario está activo (POST /auth/renovar).
/// </summary>
public static class SesionRms
{
    public const string EsquemaPorDefecto = "Bearer";
    public const string EsquemaGoogle = "Google";
    public const string Esquema = "Rms";
    public const string Emisor = "rms-erp";
    public const string Audiencia = "rms-erp-app";
    public static readonly TimeSpan Duracion = TimeSpan.FromHours(12);

    /// <summary>Llave de firma desde "Auth:SessionKey" (variable Auth__SessionKey). Mínimo 32 caracteres.</summary>
    public static SymmetricSecurityKey Llave(IConfiguration config, IHostEnvironment env)
    {
        var valor = config["Auth:SessionKey"];
        if (string.IsNullOrWhiteSpace(valor))
        {
            if (!env.IsDevelopment())
                throw new InvalidOperationException("Falta la configuración Auth:SessionKey (variable de entorno Auth__SessionKey).");
            // En desarrollo, sin llave configurada, se genera una por ejecución (las sesiones no sobreviven a un reinicio).
            return new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
        }
        if (valor.Length < 32)
            throw new InvalidOperationException("Auth:SessionKey debe tener al menos 32 caracteres.");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(valor));
    }

    /// <summary>Elige cómo validar el token según quién lo emitió: RMS o Google.</summary>
    public static string EsquemaPara(HttpContext ctx)
    {
        var auth = ctx.Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var jwt = new JsonWebTokenHandler().ReadJsonWebToken(auth["Bearer ".Length..].Trim());
                if (jwt.Issuer == Emisor) return Esquema;
            }
            catch (ArgumentException) { /* no es un JWT: lo rechaza el esquema de Google */ }
        }
        return EsquemaGoogle;
    }
}

public class SesionService
{
    private readonly SymmetricSecurityKey _llave;
    public SesionService(SymmetricSecurityKey llave) => _llave = llave;

    public (string Token, DateTime Expira) Emitir(string email, string? nombre, string? foto)
    {
        var ahora = DateTime.UtcNow;
        var expira = ahora.Add(SesionRms.Duracion);
        var claims = new Dictionary<string, object> { ["sub"] = email, ["email"] = email };
        if (!string.IsNullOrWhiteSpace(nombre)) claims["name"] = nombre;
        if (!string.IsNullOrWhiteSpace(foto)) claims["picture"] = foto;

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = SesionRms.Emisor,
            Audience = SesionRms.Audiencia,
            Claims = claims,
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = expira,
            SigningCredentials = new SigningCredentials(_llave, SecurityAlgorithms.HmacSha256)
        });
        return (token, expira);
    }
}
