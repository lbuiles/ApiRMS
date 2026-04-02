using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Tracker
{
    [Table("Proyectos")]
    public class Proyecto
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string LineaNegocio { get; set; } = string.Empty;

        [Required]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public Guid ClienteId { get; set; }
        
        // --- Nuevos campos de creación ---
        public string? SupervisorCliente { get; set; }
        public DateTime? FechaRespuestaCliente { get; set; } // Para calcular SLA 24h

        public Guid? ResponsableId { get; set; }

        public string Estado { get; set; } = "Pendiente";

        // --- Fechas del Workflow ---
        public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaSolicitudPermisos { get; set; }
        public DateTime? FechaEnvioPermiso { get; set; } // Para calcular SLA 48h
        public DateTime? FechaAprobacionPermisos { get; set; }
        public DateTime? FechaEnvioPresupuesto { get; set; }
        public DateTime? FechaAprobacionPresupuesto { get; set; }
        public DateTime? FechaInicioActividades { get; set; }
        public DateTime? FechaFinalizacionActividades { get; set; }

        // --- Datos Administrativos / Terceros ---
        public string? Contratista { get; set; }
        public string? Polizas { get; set; } // Ej: "Requiere", "No Requiere"
        public string? CentroDeCostos { get; set; }

        // --- Cierre Administrativo (Checklists) ---
        public bool DossierEntregado { get; set; } = false;
        public bool LiquidacionTerminada { get; set; } = false;
        public bool FacturacionCompletada { get; set; } = false;

        // --- Financiero ---
        public string? NumeroOC { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorOC { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorGasto { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorFacturado { get; set; } = 0;

        // --- Propiedades Calculadas ---
        [NotMapped]
        public decimal Utilidad => ValorFacturado - ValorGasto;
        
        [NotMapped]
        public decimal PorcentajeAvanceFinanciero => ValorOC > 0 ? (ValorFacturado / ValorOC) * 100 : 0;

        // --- Relaciones de Navegación ---
        [ForeignKey("ClienteId")]
        public Clientes.Cliente? Cliente { get; set; }

        [ForeignKey("ResponsableId")]
        public Usuario? Responsable { get; set; }

        public List<ProyectoObservacion> Observaciones { get; set; } = new List<ProyectoObservacion>();
    }
}