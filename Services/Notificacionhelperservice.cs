using Microsoft.EntityFrameworkCore;
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

            await GuardarSinFallarAsync(tipo);
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

            await GuardarSinFallarAsync(tipo);
        }

        /// <summary>
        /// Crea la notificación para cada destinatario, salvo que ya tenga una igual
        /// (mismo tipo y entidad) sin leer creada dentro de la ventana indicada.
        /// Evita repetir, por ejemplo, la misma alerta crítica cada vez que se guarda el proyecto.
        /// Los errores se registran y no se propagan: una notificación nunca debe tumbar la operación.
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

                var desde = DateTime.UtcNow - ventana;
                var yaAvisados = await _db.Notificaciones
                    .Where(n => ids.Contains(n.UsuarioDestinoId) && n.Tipo == tipo && n.EntidadId == entidadId
                                && !n.Leida && n.FechaCreacion >= desde)
                    .Select(n => n.UsuarioDestinoId)
                    .Distinct()
                    .ToListAsync();

                var nuevos = ids.Except(yaAvisados).ToList();
                if (nuevos.Count == 0) return;

                foreach (var uid in nuevos)
                {
                    _db.Notificaciones.Add(new Notificacion
                    {
                        UsuarioDestinoId = uid,
                        Tipo             = tipo,
                        Mensaje          = mensaje.Length > 500 ? mensaje[..497] + "..." : mensaje,
                        EntidadId        = entidadId,
                        Leida            = false,
                        FechaCreacion    = DateTime.UtcNow
                    });
                }
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                DescartarPendientes();
                Console.WriteLine($"No se pudo crear la notificación '{tipo}': {ex.Message}");
            }
        }

        /// <summary>
        /// Guarda las notificaciones agregadas. Si falla (p. ej. la tabla no existe),
        /// las descarta y lo registra: la operación principal ya se guardó y no debe fallar por esto.
        /// </summary>
        private async Task GuardarSinFallarAsync(string tipo)
        {
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                DescartarPendientes();
                Console.WriteLine($"No se pudo crear la notificación '{tipo}': {ex.Message}");
            }
        }

        private void DescartarPendientes()
        {
            foreach (var e in _db.ChangeTracker.Entries<Notificacion>().Where(e => e.State == EntityState.Added).ToList())
                e.State = EntityState.Detached;
        }
    }
}
