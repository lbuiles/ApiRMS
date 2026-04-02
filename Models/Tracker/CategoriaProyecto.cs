using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Tracker
{
    [Table("Proyectos_Categorias")]
    public class CategoriaProyecto
    {
        [Key]
        public string Id { get; set; } = string.Empty; // Ej: 'civiles', 'energia'
        
        [Required]
        public string Nombre { get; set; } = string.Empty;
        
        public string Descripcion { get; set; } = string.Empty;
        
        [Required]
        public string Icono { get; set; } = string.Empty; // El Path del SVG
        
        public int CantidadActivos { get; set; } = 0;
        
        public decimal PresupuestoTotal { get; set; } = 0;
        
        public string ColorTema { get; set; } = "text-[#1e3a8a]";
        
        public string BgTema { get; set; } = "bg-blue-50";
        
        public string PermisoRequerido { get; set; } = string.Empty;
    }
}