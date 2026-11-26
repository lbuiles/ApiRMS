using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace RmsErp.Api.Controllers
{
    /// <summary>
    /// Entrega los archivos subidos (antes se servían públicamente en /uploads sin sesión).
    /// Exige usuario registrado y activo (política por defecto) y, para los documentos de
    /// clientes y contratistas, el permiso de lectura del módulo correspondiente.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("archivos")]
    public class ArchivosController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private static readonly FileExtensionContentTypeProvider TiposContenido = new FileExtensionContentTypeProvider();

        // Carpeta (primer nivel dentro de uploads) -> permiso requerido para leerla
        private static readonly (string carpeta, string permiso)[] PermisosPorCarpeta =
        {
            ("clientes", "admin.clientes.leer"),
            ("contratistas", "admin.contratistas.leer"),
        };

        public ArchivosController(IWebHostEnvironment env) => _env = env;

        /// <summary>GET /archivos?ruta=/uploads/clientes/xxx.pdf</summary>
        [HttpGet]
        public IActionResult Obtener([FromQuery] string ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) return BadRequest(new { message = "Falta la ruta del archivo." });

            var relativa = ruta.Replace('\\', '/').TrimStart('/');
            if (relativa.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                relativa = relativa.Substring("uploads/".Length);

            // Evita salir de la carpeta uploads (../, rutas absolutas, etc.)
            var baseUploads = System.IO.Path.GetFullPath(System.IO.Path.Combine(_env.ContentRootPath, "uploads"));
            var completa = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseUploads, relativa));
            if (!completa.StartsWith(baseUploads + System.IO.Path.DirectorySeparatorChar, StringComparison.Ordinal))
                return BadRequest(new { message = "Ruta no válida." });

            var carpeta = relativa.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
            var regla = PermisosPorCarpeta.FirstOrDefault(p => string.Equals(p.carpeta, carpeta, StringComparison.OrdinalIgnoreCase));
            if (regla.permiso != null && !User.HasClaim("permission", regla.permiso))
                return Forbid();

            if (!System.IO.File.Exists(completa)) return NotFound();

            if (!TiposContenido.TryGetContentType(completa, out var tipo)) tipo = "application/octet-stream";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            // Seguridad: solo PDF e imágenes se muestran en el navegador; cualquier otro tipo
            // (incluidos archivos antiguos como .html o .svg) se entrega como descarga.
            var seMuestra = tipo == "application/pdf" || tipo == "image/jpeg" || tipo == "image/png" || tipo == "image/webp";
            if (!seMuestra)
                return PhysicalFile(completa, "application/octet-stream", System.IO.Path.GetFileName(completa), enableRangeProcessing: true);

            return PhysicalFile(completa, tipo, enableRangeProcessing: true);
        }
    }
}
