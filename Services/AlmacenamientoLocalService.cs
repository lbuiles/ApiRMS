using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RmsErp.Api.Services
{
    public class AlmacenamientoLocalService : IAlmacenamientoService
    {
        private readonly IWebHostEnvironment _env;

        public AlmacenamientoLocalService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> GuardarArchivoAsync(IFormFile archivo, string subCarpeta)
        {
            if (archivo == null || archivo.Length == 0)
                throw new ArgumentException("El archivo está vacío o corrupto.");

            // Usamos System.IO.Path explícitamente para evitar choques con HotChocolate
            string rutaBaseUploads = System.IO.Path.Combine(_env.ContentRootPath, "uploads");
            string rutaDirectorio = System.IO.Path.Combine(rutaBaseUploads, subCarpeta);

            if (!Directory.Exists(rutaDirectorio))
            {
                Directory.CreateDirectory(rutaDirectorio);
            }

            // Generar nombre único
            string extension = System.IO.Path.GetExtension(archivo.FileName);
            string nombreUnico = $"{Guid.NewGuid()}{extension}";
            string rutaCompleta = System.IO.Path.Combine(rutaDirectorio, nombreUnico);

            // Guardar físicamente
            using (var stream = new FileStream(rutaCompleta, FileMode.Create))
            {
                await archivo.CopyToAsync(stream);
            }

            // Retornar la URL relativa
            return $"/uploads/{subCarpeta}/{nombreUnico}";
        }
    }
}