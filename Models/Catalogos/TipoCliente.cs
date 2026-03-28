using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Catalogos
{
    [Table("Cat_TiposCliente")]
    public class TipoCliente {
        [Key] public int Id { get; set; }
        [Required] public string Nombre { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}