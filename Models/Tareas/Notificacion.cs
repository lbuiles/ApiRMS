using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tareas
{
    public class Notificacion
    {
        public int    Id               { get; set; }
        public Guid   UsuarioDestinoId { get; set; }
        public string Tipo             { get; set; } = null!; 
        public string Mensaje          { get; set; } = null!;
        public Guid?  EntidadId        { get; set; }      
        public bool   Leida            { get; set; } = false;
        public DateTime FechaCreacion  { get; set; } = DateTime.UtcNow;

        // Navigation
        public Usuario UsuarioDestino { get; set; } = null!;
    }
}