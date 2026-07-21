using HotChocolate;
using HotChocolate.Authorization;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Contratistas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.EntityState;

namespace RmsErp.Api.Mutations.Contratistas
{
    // ==========================================
    // INPUTS
    // ==========================================

    public record CPDocumentoInput(string TipoDocumento, string Url);

    public record CPAccionistaInput(
        string NombreRazonSocial,
        string TipoDocumento,
        string NumeroDocumento,
        bool TieneCategoriaPEP
    );

    public record CPCuentaBancariaInput(
        string TipoProducto,
        string NumeroCuenta,
        string Entidad,
        string? Ciudad,
        string? Departamento,
        string? Pais,
        string? Observaciones
    );

    public record CPReferenciaInput(
        string Entidad,
        string? NombreContacto,
        string? Telefono
    );

    public record ContratistaProveedorInput(
        // Clasificación
        string TipoTercero,
        string TipoPersona,
        string EstadoRegistro,
        DateTime? FechaDiligenciamiento,

        // §1 Persona Natural
        string? NombreCompleto,
        string? TipoDocumento,
        string? NumeroDocumento,
        DateTime? FechaExpedicion,
        string? LugarExpedicion,
        string? Nacionalidad,
        string? EstadoCivil,
        string? NumeroContacto,
        string? CorreoElectronico,
        string? DireccionResidencia,
        string? Ciudad,
        string? Departamento,
        string? ActividadEconomica,
        string? CodigoCIIU,
        int? EmpleadosACargo,

        // §2 Persona Jurídica
        string? RazonSocial,
        string? NIT,
        string? DigitoVerificador,
        string? DireccionEmpresa,
        string? PaisEmpresa,
        string? CiudadEmpresa,
        string? DepartamentoEmpresa,
        string? ActividadEconomicaEmpresa,
        string? CodigoCIIUEmpresa,
        string? EmailEmpresa,
        string? TelefonoEmpresa,
        string? TipoEmpresa,
        string? PaginaWeb,
        string? NombreRepresentanteLegal,
        string? DocumentoRepresentanteLegal,

        // PEP
        bool EsPEP,
        bool AdministraRecursosPublicos,
        bool PagoConRecursosPublicos,
        bool SancionadoLavadoActivos,
        bool TieneVinculoPEP,
        string? VinculoPEPNombre,
        string? VinculoPEPDocumento,
        string? VinculoPEPParentesco,

        // §3 Tributaria
        bool EsAgenteRetencion,
        bool EsGranContribuyente,
        string? ResolucionGranContribuyente,
        bool EsAutoretenedor,
        string? ResolucionAutoretenedor,
        bool EsNoResponsableIVA,
        bool EsRegimenSimple,
        bool EsRegimenEspecial,
        string? CualRegimenEspecial,
        bool EsRegimenComun,
        bool ObligadoFacturacionElectronica,
        bool EsDeclaranteRenta,

        // §4 Financiera
        decimal? IngresosMensuales,
        decimal? EgresosMensuales,
        decimal? TotalActivos,
        decimal? TotalPasivos,
        decimal? Patrimonio,
        decimal? OtrosIngresos,
        string? ConceptoOtrosIngresos,

        // §5 Internacional
        bool PoseeCuentasExterior,
        string? PaisCuentaExterior,

        // §7 Declaración
        string? DeclaracionOrigenFondos,

        // §10 Documentos recibidos
        bool DocIdentificacion,
        bool DocRUT,
        bool DocCertificacionBancaria,
        bool DocPlanillaSeguridadSocial,
        bool DocReferenciasComerciales,
        bool DocDeclaracionRenta,
        bool DocAutorizacionDatos,
        bool DocCertificadoExistencia,
        bool DocEstadosFinancieros,

        // Relaciones
        List<CPAccionistaInput>? Accionistas,
        List<CPCuentaBancariaInput>? CuentasBancarias,
        List<CPReferenciaInput>? Referencias,
        List<CPDocumentoInput>? Documentos
    );

    // ==========================================
    // MUTATIONS
    // ==========================================

    [ExtendObjectType("Mutation")]
    public class ContratistaProveedorMutation
    {
        [Authorize(Policy = "admin.contratistas.crear")]
        public async Task<ContratistaProveedor> AddContratistaProveedor(
            ContratistaProveedorInput input,
            [Service] ApplicationDbContext context)
        {
            // Validación: debe tener nombre (PN) o razón social (PJ)
            if (input.TipoPersona == "Natural" && string.IsNullOrWhiteSpace(input.NombreCompleto))
                throw new GraphQLException("Campo obligatorio: el Nombre Completo es requerido para Persona Natural.");

            if (input.TipoPersona == "Jurídica" && string.IsNullOrWhiteSpace(input.RazonSocial))
                throw new GraphQLException("Campo obligatorio: la Razón Social es requerida para Persona Jurídica.");

            var tercero = MapInputToModel(new ContratistaProveedor(), input);
            tercero.FechaCreacion = DateTime.UtcNow;

            // Relaciones
            if (input.Accionistas != null)
                foreach (var a in input.Accionistas)
                    tercero.Accionistas.Add(new CPAccionista {
                        NombreRazonSocial = a.NombreRazonSocial,
                        TipoDocumento = a.TipoDocumento,
                        NumeroDocumento = a.NumeroDocumento,
                        TieneCategoriaPEP = a.TieneCategoriaPEP
                    });

            if (input.CuentasBancarias != null)
                foreach (var c in input.CuentasBancarias)
                    tercero.CuentasBancarias.Add(new CPCuentaBancaria {
                        TipoProducto = c.TipoProducto,
                        NumeroCuenta = c.NumeroCuenta,
                        Entidad = c.Entidad,
                        Ciudad = c.Ciudad,
                        Departamento = c.Departamento,
                        Pais = c.Pais ?? "Colombia",
                        Observaciones = c.Observaciones
                    });

            if (input.Referencias != null)
                foreach (var r in input.Referencias)
                    tercero.Referencias.Add(new CPReferencia {
                        Entidad = r.Entidad,
                        NombreContacto = r.NombreContacto,
                        Telefono = r.Telefono
                    });

            if (input.Documentos != null)
                foreach (var d in input.Documentos)
                    tercero.Documentos.Add(new CPDocumento {
                        TipoDocumento = d.TipoDocumento,
                        Url = d.Url
                    });

            context.ContratistaProveedores.Add(tercero);
            await context.SaveChangesAsync();
            return tercero;
        }

        [Authorize(Policy = "admin.contratistas.editar")]
        public async Task<ContratistaProveedor> UpdateContratistaProveedor(
            Guid id,
            ContratistaProveedorInput input,
            [Service] IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            using var context = await contextFactory.CreateDbContextAsync();

            var tercero = await context.ContratistaProveedores
                .Include(t => t.Accionistas)
                .Include(t => t.CuentasBancarias)
                .Include(t => t.Referencias)
                .Include(t => t.Documentos)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tercero == null)
                throw new GraphQLException("Contratista/Proveedor no encontrado.");

            // Capturar colecciones existentes ANTES de MapInputToModel
            var accionistasExistentes  = tercero.Accionistas.ToList();
            var cuentasExistentes      = tercero.CuentasBancarias.ToList();
            var referenciasExistentes  = tercero.Referencias.ToList();
            var docsExistentes         = tercero.Documentos.ToList();
            var urlsActuales           = docsExistentes.Select(d => d.Url).ToHashSet();

            // Actualizar campos escalares de la entidad principal
            MapInputToModel(tercero, input);

            // Después de MapInputToModel, EF puede haber marcado las entidades
            // relacionadas como Modified. Las reseteamos a Unchanged para que
            // no genere UPDATEs no deseados.
            foreach (var e in accionistasExistentes)  context.Entry(e).State = EntityState.Unchanged;
            foreach (var e in cuentasExistentes)      context.Entry(e).State = EntityState.Unchanged;
            foreach (var e in referenciasExistentes)  context.Entry(e).State = EntityState.Unchanged;
            foreach (var e in docsExistentes)         context.Entry(e).State = EntityState.Unchanged;

            // Guardar campos escalares primero
            await context.SaveChangesAsync();

            // Sync Accionistas: eliminar y recrear
            context.CPAccionistas.RemoveRange(accionistasExistentes);
            await context.SaveChangesAsync();
            if (input.Accionistas != null)
                foreach (var a in input.Accionistas)
                    context.CPAccionistas.Add(new CPAccionista {
                        ContratistProveedorId = tercero.Id,
                        NombreRazonSocial = a.NombreRazonSocial,
                        TipoDocumento = a.TipoDocumento,
                        NumeroDocumento = a.NumeroDocumento,
                        TieneCategoriaPEP = a.TieneCategoriaPEP
                    });
            await context.SaveChangesAsync();

            // Sync CuentasBancarias
            context.CPCuentasBancarias.RemoveRange(cuentasExistentes);
            await context.SaveChangesAsync();
            if (input.CuentasBancarias != null)
                foreach (var c in input.CuentasBancarias)
                    context.CPCuentasBancarias.Add(new CPCuentaBancaria {
                        ContratistProveedorId = tercero.Id,
                        TipoProducto = c.TipoProducto,
                        NumeroCuenta = c.NumeroCuenta,
                        Entidad = c.Entidad,
                        Ciudad = c.Ciudad,
                        Departamento = c.Departamento,
                        Pais = c.Pais ?? "Colombia",
                        Observaciones = c.Observaciones
                    });
            await context.SaveChangesAsync();

            // Sync Referencias
            context.CPReferencias.RemoveRange(referenciasExistentes);
            await context.SaveChangesAsync();
            if (input.Referencias != null)
                foreach (var r in input.Referencias)
                    context.CPReferencias.Add(new CPReferencia {
                        ContratistProveedorId = tercero.Id,
                        Entidad = r.Entidad,
                        NombreContacto = r.NombreContacto,
                        Telefono = r.Telefono
                    });
            await context.SaveChangesAsync();

            // Sync Documentos — solo insertar nuevos, la eliminación es individual
            if (input.Documentos != null)
                foreach (var d in input.Documentos.Where(d => !urlsActuales.Contains(d.Url)))
                    context.CPDocumentos.Add(new CPDocumento {
                        ContratistProveedorId = tercero.Id,
                        TipoDocumento = d.TipoDocumento,
                        Url = d.Url
                    });
            await context.SaveChangesAsync();

            return tercero;
        }
        [Authorize(Policy = "admin.contratistas.editar")]
        public async Task<bool> DeleteCPDocumento(
            Guid id,
            [Service] ApplicationDbContext context)
        {
            var doc = await context.CPDocumentos.FindAsync(id);
            if (doc == null) return false;
            context.CPDocumentos.Remove(doc);
            await context.SaveChangesAsync();
            return true;
        }

        [Authorize(Policy = "admin.contratistas.borrar")]
        public async Task<bool> DeleteContratistaProveedor(
            Guid id,
            [Service] ApplicationDbContext context)
        {
            var tercero = await context.ContratistaProveedores.FindAsync(id);
            if (tercero == null) return false;
            // Inactivar en lugar de eliminar físicamente
            tercero.Estado = "INACTIVO";
            await context.SaveChangesAsync();
            return true;
        }

        [Authorize(Policy = "admin.contratistas.editar")]
        public async Task<bool> ActivateContratistaProveedor(
            Guid id,
            [Service] ApplicationDbContext context)
        {
            var tercero = await context.ContratistaProveedores.FindAsync(id);
            if (tercero == null) return false;
            tercero.Estado = "ACTIVO";
            await context.SaveChangesAsync();
            return true;
        }

        // Mapeo compartido entre Add y Update
        private static ContratistaProveedor MapInputToModel(ContratistaProveedor m, ContratistaProveedorInput i)
        {
            m.TipoTercero = i.TipoTercero; m.TipoPersona = i.TipoPersona;
            m.EstadoRegistro = i.EstadoRegistro; m.FechaDiligenciamiento = i.FechaDiligenciamiento;
            m.NombreCompleto = i.NombreCompleto; m.TipoDocumento = i.TipoDocumento;
            m.NumeroDocumento = i.NumeroDocumento; m.FechaExpedicion = i.FechaExpedicion;
            m.LugarExpedicion = i.LugarExpedicion; m.Nacionalidad = i.Nacionalidad;
            m.EstadoCivil = i.EstadoCivil; m.NumeroContacto = i.NumeroContacto;
            m.CorreoElectronico = i.CorreoElectronico; m.DireccionResidencia = i.DireccionResidencia;
            m.Ciudad = i.Ciudad; m.Departamento = i.Departamento;
            m.ActividadEconomica = i.ActividadEconomica; m.CodigoCIIU = i.CodigoCIIU;
            m.EmpleadosACargo = i.EmpleadosACargo;
            m.RazonSocial = i.RazonSocial; m.NIT = i.NIT; m.DigitoVerificador = i.DigitoVerificador;
            m.DireccionEmpresa = i.DireccionEmpresa; m.PaisEmpresa = i.PaisEmpresa;
            m.CiudadEmpresa = i.CiudadEmpresa; m.DepartamentoEmpresa = i.DepartamentoEmpresa;
            m.ActividadEconomicaEmpresa = i.ActividadEconomicaEmpresa; m.CodigoCIIUEmpresa = i.CodigoCIIUEmpresa;
            m.EmailEmpresa = i.EmailEmpresa; m.TelefonoEmpresa = i.TelefonoEmpresa;
            m.TipoEmpresa = i.TipoEmpresa; m.PaginaWeb = i.PaginaWeb;
            m.NombreRepresentanteLegal = i.NombreRepresentanteLegal;
            m.DocumentoRepresentanteLegal = i.DocumentoRepresentanteLegal;
            m.EsPEP = i.EsPEP; m.AdministraRecursosPublicos = i.AdministraRecursosPublicos;
            m.PagoConRecursosPublicos = i.PagoConRecursosPublicos;
            m.SancionadoLavadoActivos = i.SancionadoLavadoActivos;
            m.TieneVinculoPEP = i.TieneVinculoPEP; m.VinculoPEPNombre = i.VinculoPEPNombre;
            m.VinculoPEPDocumento = i.VinculoPEPDocumento; m.VinculoPEPParentesco = i.VinculoPEPParentesco;
            m.EsAgenteRetencion = i.EsAgenteRetencion; m.EsGranContribuyente = i.EsGranContribuyente;
            m.ResolucionGranContribuyente = i.ResolucionGranContribuyente;
            m.EsAutoretenedor = i.EsAutoretenedor; m.ResolucionAutoretenedor = i.ResolucionAutoretenedor;
            m.EsNoResponsableIVA = i.EsNoResponsableIVA; m.EsRegimenSimple = i.EsRegimenSimple;
            m.EsRegimenEspecial = i.EsRegimenEspecial; m.CualRegimenEspecial = i.CualRegimenEspecial;
            m.EsRegimenComun = i.EsRegimenComun;
            m.ObligadoFacturacionElectronica = i.ObligadoFacturacionElectronica;
            m.EsDeclaranteRenta = i.EsDeclaranteRenta;
            m.IngresosMensuales = i.IngresosMensuales; m.EgresosMensuales = i.EgresosMensuales;
            m.TotalActivos = i.TotalActivos; m.TotalPasivos = i.TotalPasivos;
            m.Patrimonio = i.Patrimonio; m.OtrosIngresos = i.OtrosIngresos;
            m.ConceptoOtrosIngresos = i.ConceptoOtrosIngresos;
            m.PoseeCuentasExterior = i.PoseeCuentasExterior; m.PaisCuentaExterior = i.PaisCuentaExterior;
            m.DeclaracionOrigenFondos = i.DeclaracionOrigenFondos;
            m.DocIdentificacion = i.DocIdentificacion; m.DocRUT = i.DocRUT;
            m.DocCertificacionBancaria = i.DocCertificacionBancaria;
            m.DocPlanillaSeguridadSocial = i.DocPlanillaSeguridadSocial;
            m.DocReferenciasComerciales = i.DocReferenciasComerciales;
            m.DocDeclaracionRenta = i.DocDeclaracionRenta; m.DocAutorizacionDatos = i.DocAutorizacionDatos;
            m.DocCertificadoExistencia = i.DocCertificadoExistencia;
            m.DocEstadosFinancieros = i.DocEstadosFinancieros;
            return m;
        }
    }
}