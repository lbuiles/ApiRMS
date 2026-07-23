using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RmsErp.Api.Data;

namespace RmsErp.Api.Queries.Tareas
{
    // ─────────────────────────────────────────────────────────────
    // DTO de respuesta
    // ─────────────────────────────────────────────────────────────
    public record NotificacionDto(
        int      Id,
        string   Tipo,
        string   Mensaje,
        Guid?    EntidadId,
        bool     Leida,
        DateTime FechaCreacion
    );

    // ─────────────────────────────────────────────────────────────
    // Query
    // ─────────────────────────────────────────────────────────────
    [ExtendObjectType("Query")]
    public class NotificacionQuery
    {
        /// <summary>
        /// Retorna las últimas 50 notificaciones del usuario autenticado,
        /// ordenadas de más reciente a más antigua.
        /// </summary>
        [Authorize]
        public async Task<List<NotificacionDto>> MisNotificaciones(
            [Service] ApplicationDbContext db,
            [Service] IHttpContextAccessor http)
        {
            var claim = http.HttpContext?.User.FindFirstValue("rms_user_id")
                        ?? http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Usuario no autenticado.");

            return await db.Notificaciones
                .Where(n => n.UsuarioDestinoId == userId)
                .OrderByDescending(n => n.FechaCreacion)
                .Take(50)
                .Select(n => new NotificacionDto(
                    n.Id,
                    n.Tipo,
                    n.Mensaje,
                    n.EntidadId,
                    n.Leida,
                    n.FechaCreacion))
                .ToListAsync();
        }
    }
}