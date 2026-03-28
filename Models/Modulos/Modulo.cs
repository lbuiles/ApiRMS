using System.ComponentModel.DataAnnotations;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Modulos;

public class Modulo
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string Nombre { get; set; } = string.Empty;
    
    [Required]
    public string Ruta { get; set; } = string.Empty;
    
    [Required]
    public string Icono { get; set; } = string.Empty;
    
    public int Orden { get; set; }
    
    [Required]
    public string SlugRaiz { get; set; } = string.Empty; 

    [Required]
    public string Area { get; set; } = "GENERAL";

    public virtual ICollection<Permiso> ListaPermisos { get; set; } = new List<Permiso>();
    
}