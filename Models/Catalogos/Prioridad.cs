using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Catalogos
{
    [Table("Cat_Prioridades")]
    public class Prioridad {
        [Key] public int Id { get; set; }
        [Required] public string Nombre { get; set; } = string.Empty;
        public int Nivel { get; set; }
        public bool Activo { get; set; } = true;
    }
}