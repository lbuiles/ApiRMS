using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Catalogos
{
    [Table("Cat_Monedas")]
    public class Moneda {
        [Key] public int Id { get; set; }
        [Required] public string Codigo { get; set; } = string.Empty;
        [Required] public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }

}