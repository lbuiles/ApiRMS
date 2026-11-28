using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Tareas;
using RmsErp.Api.Services.Push;

namespace RmsErp.Api.Services
{
    /// <summary>
    /// Servicio auxiliar para crear notificaciones desde cualquier mutación.
    /// Cada notificación guardada también se envía como push a los dispositivos suscritos del usuario.
    /// Nunca lanza excepciones: una notificación no debe hacer fallar la operación principal.
    /// </summary>
    public class NotificacionHelperService
    {
        private readonly ApplicationDbContext _db;
        private readonly PushCola _push;

        public NotificacionHelperService(ApplicationDbContext db, PushCola push)
        {
            _db = db;
            _push = push;
        }

        /// <summary>
        /// Crea una notificación para un usuario destino.
        /// </summary>
        /// <param name="usuarioDestinoId">Guid del usuario que recibirá la notificación</param>
        /// <param name="tipo">Tipo: 'estado_cambiado' | 'asignacion' | 'eliminacion' | 'proy_*'</param>
        /// <param name="mensaje">Texto descriptivo de la notificación</param>
        /// <param name="entidadId">Id de la tarea o proyecto relacionado (opcional)</param>
        public Task CrearAsync(
            Guid   usuarioDestinoId,
            string tipo,
            string mensaje,
            Guid?  entidadId = null)
            => GuardarAsync(new[] { usuarioDestinoId }, tipo, mensaje, entidadId);

        /// <summary>
        /// Crea notificaciones en lote para múltiples usuarios (una transacción).
        /// </summary>
        public Task CrearVariasAsync(
            IEnumerable<Guid> usuariosDestinoIds,
            string tipo,
            string mensaje,
            Guid?  entidadId = null)
            => GuardarAsync(usuariosDestinoIds, tipo, mensaje, entidadId);

        /// <summary>
        /// Crea la notificación para cada destinatario, salvo que ya tenga una igual
        /// (mismo tipo y entidad) sin leer creada dentro de la ventana indicada.
        /// Evita repetir, por ejemplo, la misma alerta crítica cada vez que se guarda el proyecto.
        /// </summary>
        public async Task CrearSinRepetirAsync(
            IEnumerable<Guid> usuariosDestinoIds,
            string   tipo,
            string   mensaje,
            Guid?    entidadId,
            TimeSpan ventana)
        {
            try
            {
                var ids = usuariosDestinoIds.Where(g => g != Guid.Empty).Distinct().ToList();
                if (ids.Count == 0) return;

                if (ventana > TimeSpan.Zero)
                {
                    var desde = DateTime.UtcNow - ventana;
                    var yaAvisados = await _db.Notificaciones
                        .Where(n => ids.Contains(n.UsuarioDestinoId) && n.Tipo == tipo && n.EntidadId == entidadId
                                    && !n.Leida && n.FechaCreacion >= desde)
                        .Select(n => n.UsuarioDestinoId)
                        .Distinct()
                        .ToListAsync();
                    ids = ids.Except(yaAvisados).ToList();
                }

                await GuardarAsync(ids, tipo, mensaje, entidadId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"No se pudo crear la notificación '{tipo}': {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────
        // Guardado común + envío push
        // ─────────────────────────────────────────────────────────
        private async Task GuardarAsync(IEnumerable<Guid> usuariosDestinoIds, string tipo, string mensaje, Guid? entidadId)
        {
            var nuevas = usuariosDestinoIds
                .Where(g => g != Guid.Empty)
                .Distinct()
                .Select(uid => new Notificacion
                {
                    UsuarioDestinoId = uid,
                    Tipo             = tipo,
                    Mensaje          = mensaje.Length > 500 ? mensaje[..497] + "..." : mensaje,
                    EntidadId        = entidadId,
                    Leida            = false,
                    FechaCreacion    = DateTime.UtcNow
                })
                .ToList();
            if (nuevas.Count == 0) return;

            try
            {
                _db.Notificaciones.AddRange(nuevas);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // P. ej. la tabla no existe: se descartan y la operación principal sigue
                foreach (var e in _db.ChangeTracker.Entries<Notificacion>().Where(e => e.State == EntityState.Added).ToList())
                    e.State = EntityState.Detached;
                Console.WriteLine($"No se pudo crear la notificación '{tipo}': {ex.Message}");
                return;
            }

            // Push (en segundo plano): al tocarlo, el ERP abre /panel?notificacion=<id> y la campana la abre
            foreach (var n in nuevas)
                _push.Encolar(new PushMensaje(
                    n.UsuarioDestinoId,
                    TituloPush(tipo),
                    n.Mensaje,
                    $"/panel?notificacion={n.Id}",
                    entidadId.HasValue ? $"{tipo}-{entidadId}" : null));
        }

        private static string TituloPush(string tipo) => tipo switch
        {
            "proy_asignado"   => "Nuevo proyecto a tu cargo",
            "proy_estado"     => "Cambio de estado de proyecto",
            "proy_bitacora"   => "Nueva observación en un proyecto",
            "proy_alerta"     => "Alerta crítica de proyecto",
            "asignacion"      => "Nueva tarea asignada",
            "estado_cambiado" => "Tarea actualizada",
            "eliminacion"     => "Tarea eliminada",
            _                 => "ERP RMS"
        };
    }
}
