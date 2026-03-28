using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Clientes
{
    [Table("Clientes_Contactos")]
    public class ClienteContacto
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Nombre { get; set; } = string.Empty;
        
        public string Cargo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;

        // Relación con la Sucursal (Padre)
        [Required]
        public Guid SucursalId { get; set; }

        [ForeignKey("SucursalId")]
        public ClienteSucursal? Sucursal { get; set; }
    }
}