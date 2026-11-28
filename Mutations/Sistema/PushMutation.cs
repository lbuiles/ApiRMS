using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Tareas;
using RmsErp.Api.Services;
using RmsErp.Api.Services.Push;

namespace RmsErp.Api.Mutations.Sistema
{
    public record PushSuscripcionInput(string Endpoint, string P256dh, string Auth, string? Dispositivo);

    [ExtendObjectType("Mutation")]
    public class PushMutation
    {
        /// <summary>
        /// Registra (o reasigna al usuario actual) la suscripción push de este navegador.
        /// Si el mismo navegador estaba suscrito con otro usuario, pasa a ser del usuario actual.
        /// </summary>
        [Authorize]
        public async Task<bool> SuscribirPush(
            PushSuscripcionInput input,
            [Service] ApplicationDbContext db,
            [Service] IHttpContextAccessor http)
        {
            var usuarioId = ProyectoAcceso.Contexto(http).usuarioId;
            if (usuarioId == Guid.Empty) throw new GraphQLException("Usuario no autenticado.");

            if (!Uri.TryCreate(input.Endpoint, UriKind.Absolute, out var uri) || uri.Scheme != "https" || input.Endpoint.Length > 800)
                throw new GraphQLException("La suscripción push no es válida.");
            if (string.IsNullOrWhiteSpace(input.P256dh) || input.P256dh.Length > 200 ||
                string.IsNullOrWhiteSpace(input.Auth) || input.Auth.Length > 100)
                throw new GraphQLException("La suscripción push no es válida.");

            var s = await db.PushSuscripciones.FirstOrDefaultAsync(x => x.Endpoint == input.Endpoint);
            if (s == null)
            {
                s = new PushSuscripcion { Endpoint = input.Endpoint };
                db.PushSuscripciones.Add(s);
            }
            s.UsuarioId     = usuarioId;
            s.P256dh        = input.P256dh;
            s.Auth          = input.Auth;
            s.Dispositivo   = input.Dispositivo is { Length: > 200 } d ? d[..200] : input.Dispositivo;
            s.FechaCreacion = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return true;
        }

        /// <summary>Quita la suscripción de este navegador (al desactivar o al cerrar sesión).</summary>
        [Authorize]
        public async Task<bool> DesuscribirPush(
            string endpoint,
            [Service] ApplicationDbContext db,
            [Service] IHttpContextAccessor http)
        {
            var usuarioId = ProyectoAcceso.Contexto(http).usuarioId;
            var borradas = await db.PushSuscripciones
                .Where(x => x.Endpoint == endpoint && x.UsuarioId == usuarioId)
                .ExecuteDeleteAsync();
            return borradas > 0;
        }

        /// <summary>
        /// Envía una notificación de prueba al usuario actual (queda también en la campana).
        /// Devuelve cuántos dispositivos tiene suscritos.
        /// </summary>
        [Authorize]
        public async Task<int> ProbarPush(
            [Service] ApplicationDbContext db,
            [Service] IHttpContextAccessor http,
            [Service] NotificacionHelperService notificaciones)
        {
            var usuarioId = ProyectoAcceso.Contexto(http).usuarioId;
            await notificaciones.CrearAsync(usuarioId, "prueba",
                "Notificación de prueba: si la ves en este dispositivo, las notificaciones push funcionan.");
            return await db.PushSuscripciones.CountAsync(x => x.UsuarioId == usuarioId);
        }
    }
}
