using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Catalogos;

namespace RmsErp.Api.Models.Clientes
{
    [Table("Clientes")]
    public class Cliente
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();        
        
        [Required]
        public string RazonSocial { get; set; } = string.Empty;
        
        [Required]
        public string Nit { get; set; } = string.Empty;
        
        public string Departamento { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactoPrincipal { get; set; } = string.Empty;
        public string Estado { get; set; } = "ACTIVO";
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        
        [Required]
        public string UsuarioCreacion { get; set; } = "SISTEMA";

        public int? TipoClienteId { get; set; }
        [ForeignKey("TipoClienteId")] 
        public virtual TipoCliente? TipoCliente { get; set; }


        public virtual ClienteCondicion? Condiciones { get; set; }
        public virtual ClienteOperacion? Operacion { get; set; }

        public virtual ICollection<ClienteSucursal> Sucursales { get; set; } = new List<ClienteSucursal>();

        public virtual ICollection<ClientePoliza> ClientePolizas { get; set; } = new List<ClientePoliza>();
        public virtual ICollection<ClienteServicio> ClienteServicios { get; set; } = new List<ClienteServicio>();
        public virtual ICollection<ClienteRegion> ClienteRegiones { get; set; } = new List<ClienteRegion>();
        public virtual ICollection<ClienteDocumento> Documentos { get; set; } = new List<ClienteDocumento>();
    }
}