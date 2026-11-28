using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using RmsErp.Api.Data;
using RmsErp.Api.Services;

namespace RmsErp.Api.Mutations.Sistema
{
    [ExtendObjectType("Mutation")]
    public class PerfilMutation
    {
        /// <summary>
        /// Guarda la foto de perfil de Google del usuario actual (Usuario.AvatarUrl), para que
        /// al iniciar sesión en cualquier equipo se vea sin volver a pedir permisos a Google.
        /// Solo acepta imágenes de Google (googleusercontent.com) por HTTPS.
        /// </summary>
        [Authorize]
        public async Task<bool> ActualizarMiFoto(
            string url,
            [Service] ApplicationDbContext db,
            [Service] IHttpContextAccessor http)
        {
            var usuarioId = ProyectoAcceso.Contexto(http).usuarioId;
            if (usuarioId == Guid.Empty) throw new GraphQLException("Usuario no autenticado.");

            url = (url ?? "").Trim();
            // Las fotos de cuentas de Google Workspace pueden tener direcciones muy largas (más de 1.000 caracteres)
            if (url.Length > 4000)
                throw new GraphQLException($"La dirección de la foto es demasiado larga ({url.Length} caracteres).");
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new GraphQLException($"La foto debe ser una dirección https válida (se recibió: {(url.Length > 60 ? url[..60] + "..." : url)}).");

            // Dominios donde Google sirve las fotos de perfil
            string[] dominios = { "googleusercontent.com", "ggpht.com", "google.com", "gstatic.com" };
            var host = uri.IdnHost.ToLowerInvariant();
            if (!dominios.Any(d => host == d || host.EndsWith("." + d)))
                throw new GraphQLException($"La foto debe ser una imagen de la cuenta de Google (se recibió {host}).");

            var usuario = await db.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return false;
            if (usuario.AvatarUrl == url) return true;

            usuario.AvatarUrl = url;
            await db.SaveChangesAsync();
            return true;
        }
    }
}
