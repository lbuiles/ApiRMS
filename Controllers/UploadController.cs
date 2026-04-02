using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RmsErp.Api.Services;
using System;
using System.Threading.Tasks;

namespace RmsErp.Api.Controllers
{
    [ApiController]
    [Route("[controller]")] 
    public class UploadController : ControllerBase
    {
        private readonly IAlmacenamientoService _almacenamiento;

        public UploadController(IAlmacenamientoService almacenamiento)
        {
            _almacenamiento = almacenamiento;
        }

        [HttpPost("documento")]
        public async Task<IActionResult> SubirDocumento(IFormFile archivo, [FromForm] string modulo = "general")
        {
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