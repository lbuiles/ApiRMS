using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RmsErp.Api.Models.Contratistas
{
    [Table("Contratistas_Proveedores")]
    public class ContratistaProveedor
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaDiligenciamiento { get; set; }

        [Required, MaxLength(30)]
        public string TipoTercero { get; set; } = string.Empty;   // Proveedor | Contratista | Ambos

        [Required, MaxLength(20)]
        public string TipoPersona { get; set; } = string.Empty;   // Natural | Jurídica

        [MaxLength(10)]
        public string Estado { get; set; } = "ACTIVO";            // ACTIVO | INACTIVO

        [MaxLength(20)]
        public string EstadoRegistro { get; set; } = "Nuevo";     // Nuevo | Actualización

        // §1 Persona Natural
        [MaxLength(200)] public string? NombreCompleto { get; set; }
        [MaxLength(20)]  public string? TipoDocumento { get; set; }
        [MaxLength(50)]  public string? NumeroDocumento { get; set; }
        public DateTime? FechaExpedicion { get; set; }
        [MaxLength(100)] public string? LugarExpedicion { get; set; }
        [MaxLength(80)]  public string? Nacionalidad { get; set; }
        [MaxLength(30)]  public string? EstadoCivil { get; set; }
        [MaxLength(30)]  public string? NumeroContacto { get; set; }
        [MaxLength(150)] public string? CorreoElectronico { get; set; }
        [MaxLength(200)] public string? DireccionResidencia { get; set; }
        [MaxLength(80)]  public string? Ciudad { get; set; }
        [MaxLength(80)]  public string? Departamento { get; set; }
        [MaxLength(200)] public string? ActividadEconomica { get; set; }
        [MaxLength(10)]  public string? CodigoCIIU { get; set; }
        public int? EmpleadosACargo { get; set; }

        // §2 Persona Jurídica
        [MaxLength(200)] public string? RazonSocial { get; set; }
        [MaxLength(20)]  public string? NIT { get; set; }
        [MaxLength(2)]   public string? DigitoVerificador { get; set; }
        [MaxLength(200)] public string? DireccionEmpresa { get; set; }
        [MaxLength(80)]  public string? PaisEmpresa { get; set; }
        [MaxLength(80)]  public string? CiudadEmpresa { get; set; }
        [MaxLength(80)]  public string? DepartamentoEmpresa { get; set; }
        [MaxLength(200)] public string? ActividadEconomicaEmpresa { get; set; }
        [MaxLength(10)]  public string? CodigoCIIUEmpresa { get; set; }
        [MaxLength(150)] public string? EmailEmpresa { get; set; }
        [MaxLength(30)]  public string? TelefonoEmpresa { get; set; }
        [MaxLength(30)]  public string? TipoEmpresa { get; set; }
        [MaxLength(200)] public string? PaginaWeb { get; set; }
        [MaxLength(200)] public string? NombreRepresentanteLegal { get; set; }
        [MaxLength(50)]  public string? DocumentoRepresentanteLegal { get; set; }

        // §1.2 / §2.1 PEP
        public bool EsPEP { get; set; } = false;
        public bool AdministraRecursosPublicos { get; set; } = false;
        public bool PagoConRecursosPublicos { get; set; } = false;
        public bool SancionadoLavadoActivos { get; set; } = false;
        public bool TieneVinculoPEP { get; set; } = false;
        [MaxLength(200)] public string? VinculoPEPNombre { get; set; }
        [MaxLength(50)]  public string? VinculoPEPDocumento { get; set; }
        [MaxLength(80)]  public string? VinculoPEPParentesco { get; set; }

        // §3 Tributaria
        public bool EsAgenteRetencion { get; set; } = false;
        public bool EsGranContribuyente { get; set; } = false;
        [MaxLength(100)] public string? ResolucionGranContribuyente { get; set; }
        public bool EsAutoretenedor { get; set; } = false;
        [MaxLength(100)] public string? ResolucionAutoretenedor { get; set; }
        public bool EsNoResponsableIVA { get; set; } = false;
        public bool EsRegimenSimple { get; set; } = false;
        public bool EsRegimenEspecial { get; set; } = false;
        [MaxLength(100)] public string? CualRegimenEspecial { get; set; }
        public bool EsRegimenComun { get; set; } = false;
        public bool ObligadoFacturacionElectronica { get; set; } = false;
        public bool EsDeclaranteRenta { get; set; } = false;

        // §4 Financiera
        [Column(TypeName = "decimal(18,2)")] public decimal? IngresosMensuales { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal? EgresosMensuales { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal? TotalActivos { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal? TotalPasivos { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal? Patrimonio { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal? OtrosIngresos { get; set; }
        [MaxLength(300)] public string? ConceptoOtrosIngresos { get; set; }

        // §5 Internacional
        public bool PoseeCuentasExterior { get; set; } = false;
        [MaxLength(80)] public string? PaisCuentaExterior { get; set; }

        // §7 Declaración de origen
        public string? DeclaracionOrigenFondos { get; set; }

        // §10 Documentos recibidos
        public bool DocIdentificacion { get; set; } = false;
        public bool DocRUT { get; set; } = false;
        public bool DocCertificacionBancaria { get; set; } = false;
        public bool DocPlanillaSeguridadSocial { get; set; } = false;
        public bool DocReferenciasComerciales { get; set; } = false;
        public bool DocDeclaracionRenta { get; set; } = false;
        public bool DocAutorizacionDatos { get; set; } = false;
        public bool DocCertificadoExistencia { get; set; } = false;
        public bool DocEstadosFinancieros { get; set; } = false;

        // Navegación
        public ICollection<CPAccionista> Accionistas { get; set; } = new List<CPAccionista>();
        public ICollection<CPCuentaBancaria> CuentasBancarias { get; set; } = new List<CPCuentaBancaria>();
        public ICollection<CPReferencia> Referencias { get; set; } = new List<CPReferencia>();
        public ICollection<CPDocumento> Documentos { get; set; } = new List<CPDocumento>();
    }
}