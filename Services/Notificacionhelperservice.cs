using RmsErp.Api.Data;
using RmsErp.Api.Models.Tareas;

namespace RmsErp.Api.Services
{
    /// <summary>
    /// Servicio auxiliar para crear notificaciones desde cualquier mutación.
    /// Inyectar como Scoped en las mutaciones que necesiten generar notificaciones.
    /// </summary>
    public class NotificacionHelperService
    {
        private readonly ApplicationDbContext _db;

        public NotificacionHelperService(ApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Crea una notificación para un usuario destino.
        /// </summary>
        /// <param name="usuarioDestinoId">Guid del usuario que recibirá la notificación</param>
        /// <param name="tipo">Tipo de notificación: 'estado_cambiado' | 'asignacion' | 'vencimiento'</param>
        /// <param name="mensaje">Texto descriptivo de la notificación</param>
        /// <param name="entidadId">Id de la tarea relacionada (opcional)</param>
        public async Task CrearAsync(
            Guid   usuarioDestinoId,
            string tipo,
            string mensaje,
            Guid?  entidadId = null)
        {
            _db.Notificaciones.Add(new Notificacion
            {
                UsuarioDestinoId = usuarioDestinoId,
                Tipo             = tipo,
                Mensaje          = mensaje,
                EntidadId        = entidadId,
                Leida            = false,
                FechaCreacion    = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Crea notificaciones en lote para múltiples usuarios (una transacción).
        /// </summary>
        public async Task CrearVariasAsync(
            IEnumerable<Guid> usuariosDestinoIds,
            string tipo,
            string mensaje,
            Guid?  entidadId = null)
        {
            foreach (var uid in usuariosDestinoIds.Distinct())
            {
                _db.Notificaciones.Add(new Notificacion
                {
                    UsuarioDestinoId = uid,
                    Tipo             = tipo,
                    Mensaje          = mensaje,
                    EntidadId        = entidadId,
                    Leida            = false,
                    FechaCreacion    = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
        }
    }
}