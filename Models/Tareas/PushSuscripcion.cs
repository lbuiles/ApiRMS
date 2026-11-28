using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tareas
{
    /// <summary>
    /// Suscripción de Web Push de un navegador/dispositivo de un usuario.
    /// La crea el navegador (PushManager.subscribe) y el front la registra en la API.
    /// </summary>
    public class PushSuscripcion
    {
        public int      Id             { get; set; }
        public Guid     UsuarioId      { get; set; }
        public string   Endpoint       { get; set; } = null!;   // URL del servicio push (FCM, Mozilla, Apple...)
        public string   P256dh         { get; set; } = null!;   // llave pública del navegador (base64url)
        public string   Auth           { get; set; } = null!;   // secreto de autenticación (base64url)
        public string?  Dispositivo    { get; set; }            // User-Agent resumido, para reconocerla
        public DateTime FechaCreacion  { get; set; } = DateTime.UtcNow;
        public DateTime? FechaUltimoEnvio { get; set; }

        public Usuario Usuario { get; set; } = null!;
    }
}
