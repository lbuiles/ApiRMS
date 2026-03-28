using System;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Catalogos;

namespace RmsErp.Api.Models.Clientes
{
    [Table("Clientes_Regiones")]
    public class ClienteRegion
    {
        public Guid ClienteId { get; set; }
        public virtual Cliente? Cliente { get; set; }
        public int RegionId { get; set; }
        public virtual Region? Region { get; set; }
    }
}