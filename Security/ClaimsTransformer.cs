using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using System.Security.Claims;

namespace RmsErp.Api.Security;

public class ClaimsTransformer : IClaimsTransformation
{
    private readonly IServiceProvider _serviceProvider;

    public ClaimsTransformer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.HasClaim(c => c.Type == "permission"))
        {
            return principal;
        }

        var email = principal.FindFirst(ClaimTypes.Email)?.Value 
                    ?? principal.FindFirst("email")?.Value 
                    ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

        if (string.IsNullOrEmpty(email)) return principal;

        // Seguridad: Google indica si el correo fue verificado; si viene en "false" no se reconoce al usuario
        var verificado = principal.FindFirst("email_verified")?.Value;
        if (verificado != null && !string.Equals(verificado, "true", StringComparison.OrdinalIgnoreCase))
            return principal;

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var usuario = await context.Usuarios
            .Include(u => u.UsuarioPermisos)
                .ThenInclude(up => up.Permiso)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower()); 

        // Seguridad: los usuarios INACTIVOS no reciben identidad RMS ni permisos
        if (usuario != null && string.Equals(usuario.Estado, "ACTIVO", StringComparison.OrdinalIgnoreCase))
        {
            var claimsIdentity = new ClaimsIdentity();

            foreach (var userPermiso in usuario.UsuarioPermisos)
            {
                if (userPermiso.Permiso != null)
                {
                    claimsIdentity.AddClaim(new Claim("permission", userPermiso.Permiso.Slug));
                }
            }

            claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, usuario.Nombre));
            claimsIdentity.AddClaim(new Claim("rms_user_id", usuario.Id.ToString()));

            principal.AddIdentity(claimsIdentity);
            
        }
        
        return principal;
    }
}