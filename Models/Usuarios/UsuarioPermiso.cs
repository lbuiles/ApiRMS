using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Usuarios;

public class UsuarioPermiso
{
    public Guid UsuarioId { get; set; }
    [ForeignKey("UsuarioId")]
    public Usuario Usuario { get; set; } = null!;

    public int PermisoId { get; set; }
    [ForeignKey("PermisoId")]
    public Permiso Permiso { get; set; } = null!;

    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
}