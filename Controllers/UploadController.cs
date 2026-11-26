using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RmsErp.Api.Services;
using System;
using System.Threading.Tasks;

namespace RmsErp.Api.Controllers
{
    // Seguridad: solo usuarios con sesión pueden subir archivos (antes era anónimo)
    [ApiController]
    [Authorize]
    [Route("[controller]")] 
    public class UploadController : ControllerBase
    {
        private readonly IAlmacenamientoService _almacenamiento;

        public UploadController(IAlmacenamientoService almacenamiento)
        {
            _almacenamiento = almacenamiento;
        }

        // Seguridad: solo documentos e imágenes; nada ejecutable (html, svg, js, exe...)
        private static readonly string[] ExtensionesPermitidas =
            { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".doc", ".docx", ".xls", ".xlsx" };
        private const long TamanoMaximo = 15 * 1024 * 1024; // 15 MB

        [HttpPost("documento")]
        [RequestSizeLimit(16 * 1024 * 1024)]
        public async Task<IActionResult> SubirDocumento(IFormFile archivo, [FromForm] string modulo = "general")
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "No se recibió ningún archivo." });
            if (archivo.Length > TamanoMaximo)
                return BadRequest(new { message = "El archivo supera el tamaño máximo de 15 MB." });

            var extension = System.IO.Path.GetExtension(archivo.FileName ?? "").ToLowerInvariant();
            if (Array.IndexOf(ExtensionesPermitidas, extension) < 0)
                return BadRequest(new { message = "Tipo de archivo no permitido. Se aceptan PDF, imágenes (JPG, PNG, WEBP), Word y Excel." });

            // La carpeta viene del cliente: solo letras, números, guion y guion bajo (evita rutas como ../)
            modulo = string.IsNullOrWhiteSpace(modulo) ? "general" : modulo.Trim().ToLowerInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(modulo, "^[a-z0-9_-]{1,40}$"))
                return BadRequest(new { message = "Módulo de destino no válido." });

            try
            {
                string url = await _almacenamiento.GuardarArchivoAsync(archivo, modulo);
                return Ok(new { url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}