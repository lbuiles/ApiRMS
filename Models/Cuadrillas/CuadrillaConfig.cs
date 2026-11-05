using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Cuadrillas
{
    /// <summary>Total de días compensatorios reconocidos a un técnico. Los gastados se calculan desde la agenda.</summary>
    [Table("Cuadrillas_Compensatorios")]
    public class CuadrillaCompensatorio
    {
        [Key]
        public Guid TecnicoId { get; set; }

        [Column(TypeName = "decimal(6,1)")]
        public decimal DiasAcumulados { get; set; }

        public Guid? ActualizadoPorId { get; set; }
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

        [ForeignKey("TecnicoId")]
        public Usuario? Tecnico { get; set; }
    }

    /// <summary>Base de horas. TecnicoId null = base general. Se usa la vigencia más reciente &lt;= fecha.</summary>
    [Table("Cuadrillas_ConfigHoras")]
    public class CuadrillaConfigHoras
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid? TecnicoId { get; set; }

        [Column(TypeName = "decimal(4,1)")]
        public decimal HorasSemanales { get; set; }

        public byte DiasLaborales { get; set; }

        [Column(TypeName = "date")]
        public DateTime VigenteDesde { get; set; }

        public Guid CreadoPorId { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [ForeignKey("TecnicoId")]
        public Usuario? Tecnico { get; set; }
    }

    [Table("Cuadrillas_Festivos")]
    public class CuadrillaFestivo
    {
        [Key]
        [Column(TypeName = "date")]
        public DateTime Fecha { get; set; }

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;
    }

    /// <summary>MARCADO = día improductivo agregado a mano; JUSTIFICADO = día automático que se excluye.</summary>
    [Table("Cuadrillas_DiasImproductivos")]
    public class CuadrillaDiaImproductivo
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid TecnicoId { get; set; }

        [Column(TypeName = "date")]
        public DateTime Fecha { get; set; }

        [Required, MaxLength(15)]
        public string Tipo { get; set; } = "MARCADO";

        [Required, MaxLength(300)]
        public string Motivo { get; set; } = string.Empty;

        public Guid CreadoPorId { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [ForeignKey("TecnicoId")]
        public Usuario? Tecnico { get; set; }
    }

    /// <summary>Parámetros generales del módulo Cuadrillas (clave/valor).</summary>
    [Table("Cuadrillas_Parametros")]
    public class CuadrillaParametro
    {
        [Key, MaxLength(50)]
        public string Clave { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Valor { get; set; } = string.Empty;

        public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
        public Guid? ActualizadoPorId { get; set; }
    }
}
