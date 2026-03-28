using System;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Catalogos;

namespace RmsErp.Api.Models.Clientes
{
    [Table("Clientes_Polizas")]
    public class ClientePoliza
    {
        public Guid ClienteId { get; set; }
        public virtual Cliente? Cliente { get; set; }
        public int TipoPolizaId { get; set; }
        public virtual TipoPoliza? TipoPoliza { get; set; }
    }
}