using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RmsErp.Api.Models.Clientes;
using RmsErp.Api.Models.Tracker;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Models.Cuadrillas
{
    /// <summary>
    /// Una asignación de un técnico en una fecha (actividad operativa, visita de supervisión,
    /// permiso, descanso/compensatorio, disponibilidad o actividad locativa).
    /// </summary>
    [Table("Cuadrillas_Actividades")]
    public class CuadrillaActividad
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Convención del proyecto: DateTime con columna 'date' (el frontend envía/lee 'YYYY-MM-DD')
        [Column(TypeName = "date")]
        public DateTime Fecha { get; set; }

        public Guid TecnicoId { get; set; }
        public Guid? CuadrillaId { get; set; }
        public Guid? ProyectoId { get; set; }

        // Sede interna o nombre original del Excel cuando no hay Proyecto enlazado
        [MaxLength(150)]
        public string? ProyectoReferencia { get; set; }

        // civiles | energia | telecomunicaciones (null = interno RMS)
        [MaxLength(50)]
        public string? LineaNegocio { get; set; }

        // Actividad | VisitaSupervision | Permiso | Descanso | Disponible | Locativa
        [Required, MaxLength(30)]
        public string TipoRegistro { get; set; } = "Actividad";

        [Required, MaxLength(300)]
        public string Actividad { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? OT { get; set; }

        // 'HH:mm'
        [MaxLength(5)]
        public string? HoraInicio { get; set; }

        [MaxLength(5)]
        public string? HoraFin { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? HorasPlaneadas { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? HorasReales { get; set; }

        [Required, MaxLength(20)]
        public string Prioridad { get; set; } = "Media";

        // Planeada | En ejecución | Finalizada | Bloqueada | Reprogramada | Permiso | Descanso | Disponible
        [Required, MaxLength(30)]
        public string Estado { get; set; } = "Planeada";

        // Programada | Pendiente de confirmar | Reprogramada | Cancelada
        [Required, MaxLength(30)]
        public string EstadoPlanificacion { get; set; } = "Programada";

        [MaxLength(50)]
        public string? TipoNovedad { get; set; }

        [MaxLength(1000)]
        public string? DetalleBloqueo { get; set; }

        [MaxLength(1000)]
        public string? SiguienteAccion { get; set; }

        [MaxLength(1000)]
        public string? Observaciones { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaCierre { get; set; }

        // Supervisor: CLIENTE (contacto del cliente) o INTERNO (usuario RMS)
        [MaxLength(10)]
        public string? SupervisorTipo { get; set; }
        public Guid? SupervisorContactoId { get; set; }
        public Guid? SupervisorUsuarioId { get; set; }

        [MaxLength(150)]
        public string? SupervisorNombre { get; set; }

        public Guid CreadoPorId { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public Guid? ActualizadoPorId { get; set; }
        public DateTime? FechaActualizacion { get; set; }

        // --- Navegación ---
        [ForeignKey("TecnicoId")]
        public Usuario? Tecnico { get; set; }

        [ForeignKey("CuadrillaId")]
        public Cuadrilla? Cuadrilla { get; set; }

        [ForeignKey("ProyectoId")]
        public Proyecto? Proyecto { get; set; }

        [ForeignKey("SupervisorContactoId")]
        public ClienteContacto? SupervisorContacto { get; set; }

        [ForeignKey("SupervisorUsuarioId")]
        public Usuario? SupervisorUsuario { get; set; }

        [ForeignKey("CreadoPorId")]
        public Usuario? Creador { get; set; }

        [ForeignKey("ActualizadoPorId")]
        public Usuario? ActualizadoPor { get; set; }

        public List<CuadrillaActividadHistorial> Historial { get; set; } = new List<CuadrillaActividadHistorial>();
    }

    [Table("Cuadrillas_ActividadHistorial")]
    public class CuadrillaActividadHistorial
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ActividadId { get; set; }
        public Guid? UsuarioId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(500)]
        public string Texto { get; set; } = string.Empty;

        [ForeignKey("UsuarioId")]
        public Usuario? Usuario { get; set; }
    }
}
