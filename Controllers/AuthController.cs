using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RmsErp.Api.Security;

namespace RmsErp.Api.Controllers
{
    /// <summary>
    /// Emite el token de sesión RMS. Ambos endpoints exigen además la política por defecto
    /// (usuario registrado y ACTIVO, claim rms_user_id), así que un usuario inactivo recibe 403.
    /// </summary>
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly SesionService _sesion;
        public AuthController(SesionService sesion) => _sesion = sesion;

        /// <summary>Recibe el ID token de Google (Authorization: Bearer) y devuelve el token de sesión RMS.</summary>
        [HttpPost("sesion")]
        [Authorize(AuthenticationSchemes = SesionRms.EsquemaGoogle)]
        public IActionResult IniciarSesion() => Emitir();

        /// <summary>Con un token RMS vigente devuelve uno nuevo de 12 horas.</summary>
        [HttpPost("renovar")]
        [Authorize(AuthenticationSchemes = SesionRms.Esquema)]
        public IActionResult Renovar() => Emitir();

        private IActionResult Emitir()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            if (string.IsNullOrEmpty(email)) return Unauthorized();

            var nombre = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("name")?.Value;
            var foto = User.FindFirst("picture")?.Value;
            var (token, expira) = _sesion.Emitir(email, nombre, foto);
            return Ok(new { token, expira });
        }
    }
}
