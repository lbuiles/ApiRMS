using System;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Catalogos;

namespace RmsErp.Api.Models.Clientes
{
    [Table("Clientes_Servicios")]
    public class ClienteServicio
    {
        public Guid ClienteId { get; set; }
        public virtual Cliente? Cliente { get; set; }
        public int TipoServicioId { get; set; }
        public virtual TipoServicio? TipoServicio { get; set; }
    }

}