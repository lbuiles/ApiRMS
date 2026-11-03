using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RmsErp.Api.Controllers
{
    // ==========================================================
    // Exportación a Excel (Punto 16 - Contingencia operativa)
    // Genera un respaldo .xlsx con TODOS los campos de la BD,
    // organizados en pestañas: Proyectos, OTs, Anticipos,
    // Facturación/Cortes y Costos (Requisiciones).
    // ==========================================================
    [ApiController]
    [Route("export")]
    [Authorize(Policy = "proyectos.leer")]
    public class ExportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ExportController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("proyectos")]
        public async Task<IActionResult> DescargarProyectosExcel([FromQuery] string? linea = null)
        {
            var proyectosQuery = _context.Proyectos
                .Include(p => p.Cliente)
                .Include(p => p.Responsable)
                .Include(p => p.EjecutorInterno)
                .Include(p => p.ArchivadoPor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(linea))
                proyectosQuery = proyectosQuery.Where(p => p.LineaNegocio == linea);

            var proyectos = await proyectosQuery.OrderBy(p => p.Codigo).ToListAsync();
            var proyectoIds = proyectos.Select(p => p.Id).ToList();

            var ordenesTrabajo = await _context.ProyectoOrdenesTrabajo
                .Include(o => o.Proyecto)
                .Include(o => o.TecnicoInterno)
                .Include(o => o.Creador)
                .Where(o => proyectoIds.Contains(o.ProyectoId))
                .OrderBy(o => o.Consecutivo)
                .ToListAsync();

            var anticipos = await _context.ProyectoAnticiposDirectos
                .Include(a => a.Proyecto)
                .Include(a => a.Solicitante)
                .Include(a => a.Aprobador)
                .Where(a => proyectoIds.Contains(a.ProyectoId))
                .OrderBy(a => a.FechaSolicitud)
                .ToListAsync();

            var cortes = await _context.ProyectoCortesFacturacion
                .Include(c => c.Proyecto)
                .Include(c => c.RegistradoPor)
                .Where(c => proyectoIds.Contains(c.ProyectoId))
                .OrderBy(c => c.FechaCorte)
                .ToListAsync();

            var requisiciones = await _context.ProyectoRequisiciones
                .Include(r => r.Proyecto)
                .Include(r => r.Solicitante)
                .Include(r => r.Aprobador)
                .Include(r => r.CompradoPor)
                .Include(r => r.RecibidoPor)
                .Where(r => proyectoIds.Contains(r.ProyectoId))
                .OrderBy(r => r.Consecutivo)
                .ToListAsync();

            using var wb = new XLWorkbook();

            // -------------------- HOJA: PROYECTOS --------------------
            var wsProy = wb.Worksheets.Add("Proyectos");
            string[] hProy = {
                "Código", "Nombre", "Línea de Negocio", "Cliente", "NIT Cliente", "Estado",
                "Responsable", "Ejecutor Interno", "Supervisor Cliente", "Contratista",
                "Fecha Asignación", "Fecha Respuesta Cliente", "Fecha Solicitud Permisos",
                "Fecha Visita Técnica", "Fecha Envío Permiso", "Fecha Aprobación Permisos",
                "Fecha Envío Presupuesto", "Fecha Aprobación Presupuesto", "Fecha Inicio Actividades",
                "Fecha Inicio Obra", "Fecha Tentativa Fin", "Fecha Finalización Actividades",
                "Pólizas", "Centro de Costos", "Dossier Entregado", "Liquidación Terminada",
                "Facturación Completada", "Cierre Técnico Aprobado", "Número OC", "Valor OC",
                "Valor Gasto", "Valor Facturado", "Costo Real Total", "Utilidad",
                "% Avance Financiero", "Archivado", "Fecha Archivado", "Archivado Por"
            };
            for (int i = 0; i < hProy.Length; i++) wsProy.Cell(1, i + 1).Value = hProy[i];

            int row = 2;
            foreach (var p in proyectos)
            {
                int c = 1;
                wsProy.Cell(row, c++).Value = p.Codigo;
                wsProy.Cell(row, c++).Value = p.Nombre;
                wsProy.Cell(row, c++).Value = p.LineaNegocio;
                wsProy.Cell(row, c++).Value = p.Cliente?.RazonSocial ?? "";
                wsProy.Cell(row, c++).Value = p.Cliente?.Nit ?? "";
                wsProy.Cell(row, c++).Value = p.Estado;
                wsProy.Cell(row, c++).Value = p.Responsable?.Nombre ?? "";
                wsProy.Cell(row, c++).Value = p.EjecutorInterno?.Nombre ?? "";
                wsProy.Cell(row, c++).Value = p.SupervisorCliente ?? "";
                wsProy.Cell(row, c++).Value = p.Contratista ?? "";
                wsProy.Cell(row, c++).Value = p.FechaAsignacion;
                if (p.FechaRespuestaCliente.HasValue) wsProy.Cell(row, c).Value = p.FechaRespuestaCliente.Value; c++;
                if (p.FechaSolicitudPermisos.HasValue) wsProy.Cell(row, c).Value = p.FechaSolicitudPermisos.Value; c++;
                if (p.FechaVisitaTecnica.HasValue) wsProy.Cell(row, c).Value = p.FechaVisitaTecnica.Value; c++;
                if (p.FechaEnvioPermiso.HasValue) wsProy.Cell(row, c).Value = p.FechaEnvioPermiso.Value; c++;
                if (p.FechaAprobacionPermisos.HasValue) wsProy.Cell(row, c).Value = p.FechaAprobacionPermisos.Value; c++;
                if (p.FechaEnvioPresupuesto.HasValue) wsProy.Cell(row, c).Value = p.FechaEnvioPresupuesto.Value; c++;
                if (p.FechaAprobacionPresupuesto.HasValue) wsProy.Cell(row, c).Value = p.FechaAprobacionPresupuesto.Value; c++;
                if (p.FechaInicioActividades.HasValue) wsProy.Cell(row, c).Value = p.FechaInicioActividades.Value; c++;
                if (p.FechaInicioObra.HasValue) wsProy.Cell(row, c).Value = p.FechaInicioObra.Value; c++;
                if (p.FechaTentativaFin.HasValue) wsProy.Cell(row, c).Value = p.FechaTentativaFin.Value; c++;
                if (p.FechaFinalizacionActividades.HasValue) wsProy.Cell(row, c).Value = p.FechaFinalizacionActividades.Value; c++;
                wsProy.Cell(row, c++).Value = p.Polizas ?? "";
                wsProy.Cell(row, c++).Value = p.CentroDeCostos ?? "";
                wsProy.Cell(row, c++).Value = p.DossierEntregado;
                wsProy.Cell(row, c++).Value = p.LiquidacionTerminada;
                wsProy.Cell(row, c++).Value = p.FacturacionCompletada;
                wsProy.Cell(row, c++).Value = p.CierreTecnicoAprobado;
                wsProy.Cell(row, c++).Value = p.NumeroOC ?? "";
                wsProy.Cell(row, c++).Value = p.ValorOC;
                wsProy.Cell(row, c++).Value = p.ValorGasto;
                wsProy.Cell(row, c++).Value = p.ValorFacturado;
                wsProy.Cell(row, c++).Value = p.CostoRealTotal;
                wsProy.Cell(row, c++).Value = p.Utilidad;
                wsProy.Cell(row, c++).Value = Math.Round(p.PorcentajeAvanceFinanciero, 2);
                wsProy.Cell(row, c++).Value = p.Archivado;
                if (p.FechaArchivado.HasValue) wsProy.Cell(row, c).Value = p.FechaArchivado.Value; c++;
                wsProy.Cell(row, c++).Value = p.ArchivadoPor?.Nombre ?? "";
                row++;
            }
            wsProy.SheetView.FreezeRows(1);
            wsProy.Columns().AdjustToContents();

            // -------------------- HOJA: ORDENES DE TRABAJO --------------------
            var wsOts = wb.Worksheets.Add("OTs");
            string[] hOts = {
                "Consecutivo", "Proyecto", "Contratista", "NIT Contratista", "Técnico Interno",
                "Fecha Emisión", "Valor Total", "Estado", "Alcance del Servicio", "Creador", "Firma Contratista"
            };
            for (int i = 0; i < hOts.Length; i++) wsOts.Cell(1, i + 1).Value = hOts[i];
            row = 2;
            foreach (var o in ordenesTrabajo)
            {
                int c = 1;
                wsOts.Cell(row, c++).Value = o.Consecutivo;
                wsOts.Cell(row, c++).Value = o.Proyecto?.Codigo ?? "";
                wsOts.Cell(row, c++).Value = o.ContratistaNombre ?? "";
                wsOts.Cell(row, c++).Value = o.ContratistaNit ?? "";
                wsOts.Cell(row, c++).Value = o.TecnicoInterno?.Nombre ?? "";
                wsOts.Cell(row, c++).Value = o.FechaEmision;
                wsOts.Cell(row, c++).Value = o.ValorTotal;
                wsOts.Cell(row, c++).Value = o.Estado;
                wsOts.Cell(row, c++).Value = o.AlcanceServicio;
                wsOts.Cell(row, c++).Value = o.Creador?.Nombre ?? "";
                wsOts.Cell(row, c++).Value = o.FirmaContratista;
                row++;
            }
            wsOts.SheetView.FreezeRows(1);
            wsOts.Columns().AdjustToContents();

            // -------------------- HOJA: ANTICIPOS --------------------
            var wsAnt = wb.Worksheets.Add("Anticipos");
            string[] hAnt = {
                "Proyecto", "Valor Anticipo", "Concepto", "Beneficiario", "Estado",
                "Fecha Solicitud", "Solicitante", "Aprobador", "Fecha Aprobación",
                "ID Egreso WorldOffice", "Fecha Pago", "Observaciones"
            };
            for (int i = 0; i < hAnt.Length; i++) wsAnt.Cell(1, i + 1).Value = hAnt[i];
            row = 2;
            foreach (var a in anticipos)
            {
                int c = 1;
                wsAnt.Cell(row, c++).Value = a.Proyecto?.Codigo ?? "";
                wsAnt.Cell(row, c++).Value = a.ValorAnticipo;
                wsAnt.Cell(row, c++).Value = a.Concepto;
                wsAnt.Cell(row, c++).Value = a.Beneficiario ?? "";
                wsAnt.Cell(row, c++).Value = a.Estado;
                wsAnt.Cell(row, c++).Value = a.FechaSolicitud;
                wsAnt.Cell(row, c++).Value = a.Solicitante?.Nombre ?? "";
                wsAnt.Cell(row, c++).Value = a.Aprobador?.Nombre ?? "";
                if (a.FechaAprobacion.HasValue) wsAnt.Cell(row, c).Value = a.FechaAprobacion.Value; c++;
                wsAnt.Cell(row, c++).Value = a.IdEgresoWorldOffice ?? "";
                if (a.FechaPago.HasValue) wsAnt.Cell(row, c).Value = a.FechaPago.Value; c++;
                wsAnt.Cell(row, c++).Value = a.Observaciones ?? "";
                row++;
            }
            wsAnt.SheetView.FreezeRows(1);
            wsAnt.Columns().AdjustToContents();

            // -------------------- HOJA: FACTURACIÓN / CORTES --------------------
            var wsFact = wb.Worksheets.Add("Facturacion_Cortes");
            string[] hFact = { "Proyecto", "Fecha Corte", "Valor Corte", "Número Factura", "Estado", "Registrado Por", "Observaciones" };
            for (int i = 0; i < hFact.Length; i++) wsFact.Cell(1, i + 1).Value = hFact[i];
            row = 2;
            foreach (var cf in cortes)
            {
                int c = 1;
                wsFact.Cell(row, c++).Value = cf.Proyecto?.Codigo ?? "";
                wsFact.Cell(row, c++).Value = cf.FechaCorte;
                wsFact.Cell(row, c++).Value = cf.ValorCorte;
                wsFact.Cell(row, c++).Value = cf.NumeroFactura ?? "";
                wsFact.Cell(row, c++).Value = cf.Estado;
                wsFact.Cell(row, c++).Value = cf.RegistradoPor?.Nombre ?? "";
                wsFact.Cell(row, c++).Value = cf.Observaciones ?? "";
                row++;
            }
            wsFact.SheetView.FreezeRows(1);
            wsFact.Columns().AdjustToContents();

            // -------------------- HOJA: COSTOS (REQUISICIONES) --------------------
            var wsCostos = wb.Worksheets.Add("Costos_Requisiciones");
            string[] hCostos = {
                "Consecutivo", "Proyecto", "Solicitante", "Fecha Solicitud", "Estado",
                "Valor Estimado", "Aprobador", "Fecha Aprobación", "Número OC Compra",
                "Valor Real Compra", "Comprado Por", "Fecha Compra", "Recibido Por",
                "Fecha Recibo", "Observaciones", "Observaciones Recibo"
            };
            for (int i = 0; i < hCostos.Length; i++) wsCostos.Cell(1, i + 1).Value = hCostos[i];
            row = 2;
            foreach (var r in requisiciones)
            {
                int c = 1;
                wsCostos.Cell(row, c++).Value = r.Consecutivo;
                wsCostos.Cell(row, c++).Value = r.Proyecto?.Codigo ?? "";
                wsCostos.Cell(row, c++).Value = r.Solicitante?.Nombre ?? "";
                wsCostos.Cell(row, c++).Value = r.FechaSolicitud;
                wsCostos.Cell(row, c++).Value = r.Estado;
                wsCostos.Cell(row, c++).Value = r.ValorEstimado;
                wsCostos.Cell(row, c++).Value = r.Aprobador?.Nombre ?? "";
                if (r.FechaAprobacion.HasValue) wsCostos.Cell(row, c).Value = r.FechaAprobacion.Value; c++;
                wsCostos.Cell(row, c++).Value = r.NumeroOCCompra ?? "";
                if (r.ValorRealCompra.HasValue) wsCostos.Cell(row, c).Value = r.ValorRealCompra.Value; c++;
                wsCostos.Cell(row, c++).Value = r.CompradoPor?.Nombre ?? "";
                if (r.FechaCompra.HasValue) wsCostos.Cell(row, c).Value = r.FechaCompra.Value; c++;
                wsCostos.Cell(row, c++).Value = r.RecibidoPor?.Nombre ?? "";
                if (r.FechaRecibo.HasValue) wsCostos.Cell(row, c).Value = r.FechaRecibo.Value; c++;
                wsCostos.Cell(row, c++).Value = r.Observaciones ?? "";
                wsCostos.Cell(row, c++).Value = r.ObservacionesRecibo ?? "";
                row++;
            }
            wsCostos.SheetView.FreezeRows(1);
            wsCostos.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"RMS_Tracker_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}
