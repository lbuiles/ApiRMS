using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Catalogos;

namespace RmsErp.Api.Models.Clientes
{
    [Table("Clientes_Documentos")]
    public class ClienteDocumento
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public Guid ClienteId { get; set; }
        
        [Required]
        public string TipoDocumento { get; set; } = string.Empty; // "RUT", "CamaraComercio", "Cedula"
        
        [Required]
        public string Url { get; set; } = string.Empty; // La ruta que nos dio la API
        
        public DateTime FechaSubida { get; set; } = DateTime.Now;

        [ForeignKey("ClienteId")]
        public virtual Cliente? Cliente { get; set; }
    }
}