using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;

namespace RmsErp.Api.Services.Push
{
    /// <summary>Un aviso push para un usuario (se envía a todos sus dispositivos suscritos).</summary>
    public record PushMensaje(Guid UsuarioId, string Titulo, string Cuerpo, string Url, string? Etiqueta = null);

    /// <summary>
    /// Cola en memoria: las mutaciones encolan y responden de inmediato; PushWorker envía en segundo plano.
    /// Si la API se reinicia se pierden los pendientes, pero la notificación sigue en la campana.
    /// </summary>
    public class PushCola
    {
        private readonly Channel<PushMensaje> _canal = Channel.CreateBounded<PushMensaje>(
            new BoundedChannelOptions(1000) { FullMode = BoundedChannelFullMode.DropOldest });

        public void Encolar(PushMensaje m) => _canal.Writer.TryWrite(m);
        public IAsyncEnumerable<PushMensaje> Leer(CancellationToken ct) => _canal.Reader.ReadAllAsync(ct);
    }

    public class PushWorker : BackgroundService
    {
        private readonly PushCola _cola;
        private readonly IServiceScopeFactory _scopes;
        private readonly WebPushEmisor _emisor;
        private readonly PushOpciones _op;
        private readonly ILogger<PushWorker> _log;

        public PushWorker(PushCola cola, IServiceScopeFactory scopes, WebPushEmisor emisor, PushOpciones op, ILogger<PushWorker> log)
        {
            _cola = cola; _scopes = scopes; _emisor = emisor; _op = op; _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            if (!_op.Configurado)
                _log.LogInformation("Push desactivado: faltan las llaves VAPID (Push:PublicKey / Push:PrivateKey).");

            await foreach (var m in _cola.Leer(ct))
            {
                if (!_op.Configurado) continue;
                try
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var suscripciones = await db.PushSuscripciones.Where(s => s.UsuarioId == m.UsuarioId).ToListAsync(ct);
                    if (suscripciones.Count == 0) continue;

                    // Formato que entiende el service worker de Angular (ngsw): abre/enfoca el ERP en la URL al tocarla
                    var contenido = new
                    {
                        notification = new
                        {
                            title = m.Titulo,
                            body  = m.Cuerpo,
                            icon  = "/icons/icon-192.png",
                            badge = "/icons/icon-192.png",
                            tag   = m.Etiqueta,
                            lang  = "es-CO",
                            data  = new { onActionClick = new { @default = new { operation = "navigateLastFocusedOrOpen", url = m.Url } } }
                        }
                    };

                    foreach (var s in suscripciones)
                    {
                        var r = await _emisor.EnviarAsync(s.Endpoint, s.P256dh, s.Auth, contenido, ct);
                        if (r == ResultadoPush.SuscripcionVencida) db.PushSuscripciones.Remove(s);
                        else if (r == ResultadoPush.Enviado) s.FechaUltimoEnvio = DateTime.UtcNow;
                    }
                    await db.SaveChangesAsync(ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    _log.LogWarning("Error enviando push a {Usuario}: {Mensaje}", m.UsuarioId, ex.Message);
                }
            }
        }
    }
}
