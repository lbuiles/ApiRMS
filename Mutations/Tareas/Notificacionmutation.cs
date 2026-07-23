using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RmsErp.Api.Data;
using RmsErp.Api.Queries.Tareas;

namespace RmsErp.Api.Mutations.Tareas
{
    [ExtendObjectType("Mutation")]
    public class NotificacionMutation
    {
        /// <summary>
        /// Marca una notificación específica como leída.
        /// </summary>
        [Authorize]
        public async Task<NotificacionDto> MarcarNotificacionLeida(
            int id,
            [Service] ApplicationDbContext db,
            [Service] IHttpContextAccessor http)
        {
            var userId = ObtenerUserId(http);

            var notif = await db.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioDestinoId == userId)
                ?? throw new GraphQLException("Notificación no encontrada.");

            notif.Leida = true;
            await db.SaveChangesAsync();

            return new NotificacionDto(
                notif.Id,
                notif.Tipo,
                notif.Mensaje,
                notif.EntidadId,
                notif.Leida,
                notif.FechaCreacion);
        }

        /// <summary>
        /// Marca todas las notificaciones del usuario autenticado como leídas.
        /// </summary>
        [Authorize]
        public async Task<bool> MarcarTodasNotificacionesLeidas(
            [Service] ApplicationDbContext db,
            [Service] IHttpContextAccessor http)
        {
            var userId = ObtenerUserId(http);

            await db.Notificaciones
                .Where(n => n.UsuarioDestinoId == userId && !n.Leida)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true));

            return true;
        }

        /// <summary>
        /// Elimina una notificación específica del usuario autenticado.
        /// </summary>
        [Authorize]
        public async Task<bool> EliminarNotificacion(
            int id,
            [Service] ApplicationDbContext db,
            [Service] IHttpContextAccessor http)
        {
            var userId = ObtenerUserId(http);

            var notif = await db.Notificaciones
                .FirstOrDefaultAsync(n => n.Id == id && n.UsuarioDestinoId == userId);

            if (notif == null) return false;

            db.Notificaciones.Remove(notif);
            await db.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Elimina todas las notificaciones ya leídas del usuario autenticado.
        /// </summary>
        [Authorize]
        public async Task<bool> EliminarNotificacionesLeidas(
            [Service] ApplicationDbContext db,
            [Service] IHttpContextAccessor http)
        {
            var userId = ObtenerUserId(http);

            await db.Notificaciones
                .Where(n => n.UsuarioDestinoId == userId && n.Leida)
                .ExecuteDeleteAsync();

            return true;
        }

        // ─────────────────────────────────────────────────────────
        // Helper
        // ─────────────────────────────────────────────────────────
        private static Guid ObtenerUserId(IHttpContextAccessor http)
        {
            var claim = http.HttpContext?.User.FindFirstValue("rms_user_id")
                        ?? http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Usuario no autenticado.");

            return userId;
        }
    }
}