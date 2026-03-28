using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Clientes
{
    [Table("Clientes_Sucursales")]
    public class ClienteSucursal
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Nombre { get; set; } = string.Empty;

        public string Departamento { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Estado { get; set; } = "ACTIVO";

        // Relación con el Cliente (Padre)
        [Required]
        public Guid ClienteId { get; set; }
        
        [ForeignKey("ClienteId")]
        public Cliente? Cliente { get; set; }

        // Relación con sus Contactos (Hijos)
        public List<ClienteContacto> Contactos { get; set; } = new List<ClienteContacto>();
    }
}