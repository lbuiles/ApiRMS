using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Cuadrillas
{
    [Table("Cuadrillas")]
    public class Cuadrilla
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        // civiles | energia | telecomunicaciones
        [MaxLength(50)]
        public string? LineaNegocio { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public List<CuadrillaMiembro> Miembros { get; set; } = new List<CuadrillaMiembro>();
    }

    [Table("Cuadrillas_Miembros")]
    public class CuadrillaMiembro
    {
        public Guid CuadrillaId { get; set; }
        public Guid UsuarioId { get; set; }
        public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;

        [ForeignKey("CuadrillaId")]
        public Cuadrilla? Cuadrilla { get; set; }

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }
    }
}
