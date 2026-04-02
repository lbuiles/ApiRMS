using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace RmsErp.Api.Services
{
    public interface IAlmacenamientoService
    {
        Task<string> GuardarArchivoAsync(IFormFile archivo, string subCarpeta);
    }
}