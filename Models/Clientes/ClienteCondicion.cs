using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Catalogos;

namespace RmsErp.Api.Models.Clientes
{
    [Table("Clientes_Condiciones")]
    public class ClienteCondicion
    {
        [Key] public Guid Id { get; set; }
        [Required] public Guid ClienteId { get; set; }

        public int? TipoContratoId { get; set; }
        public int? CondicionPagoId { get; set; }
        public int? MonedaId { get; set; }
        public int? TipoFacturacionId { get; set; }

        [Required] public string EmailFacturacion { get; set; } = string.Empty;
        public bool RequiereOC { get; set; } = false;
        public bool RequierePolizas { get; set; } = false;

        // Navegación
        [ForeignKey("ClienteId")] public virtual Cliente? Cliente { get; set; }
        [ForeignKey("TipoContratoId")] public virtual TipoContrato? TipoContrato { get; set; }
        [ForeignKey("CondicionPagoId")] public virtual CondicionPago? CondicionPago { get; set; }
        [ForeignKey("MonedaId")] public virtual Moneda? Moneda { get; set; }
        [ForeignKey("TipoFacturacionId")] public virtual TipoFacturacion? TipoFacturacion { get; set; }
    }
}