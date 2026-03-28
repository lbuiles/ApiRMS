using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Modulos;

namespace RmsErp.Api.Models.Usuarios;

public class Permiso
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    public string Slug { get; set; } = string.Empty;

    public int? ModuloId { get; set; } 
  
    public virtual ICollection<UsuarioPermiso> UsuarioPermisos { get; set; } = new List<UsuarioPermiso>();

    [ForeignKey("ModuloId")]
    public virtual Modulo? ModuloRelacion { get; set; } 
}