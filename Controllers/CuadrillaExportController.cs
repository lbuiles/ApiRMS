using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Cuadrillas;
using RmsErp.Api.Services;

namespace RmsErp.Api.Controllers
{
    /// <summary>
    /// Exportación a Excel del módulo Cuadrillas.
    /// Respeta la visibilidad: quien solo tiene ".ver" exporta únicamente sus actividades;
    /// quien tiene ".programar" exporta todas; las hojas de KPI requieren ".kpi".
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("cuadrilla-export")]
    public class CuadrillaExportController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly CuadrillaKpiService _kpi;

        public CuadrillaExportController(ApplicationDbContext db, CuadrillaKpiService kpi)
        {
            _db = db;
            _kpi = kpi;
        }

        [HttpGet("excel")]
        public async Task<IActionResult> Excel([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] Guid? tecnicoId = null)
        {
            desde = desde.Date;
            hasta = hasta.Date;
            if (hasta < desde) return BadRequest(new { message = "El rango de fechas no es válido." });
            if ((hasta - desde).TotalDays > 400) return BadRequest(new { message = "El rango máximo es de 400 días." });

            var idStr = User.FindFirst("rms_user_id")?.Value;
            if (!Guid.TryParse(idStr, out var usuarioId)) return Forbid();

            var tieneVer = User.HasClaim("permission", "proyectos.cuadrillas.ver");
            var verTodas = User.HasClaim("permission", "proyectos.cuadrillas.programar");
            var tieneKpi = User.HasClaim("permission", "proyectos.cuadrillas.kpi");
            if (!tieneVer && !verTodas) return Forbid();

            var query = _db.CuadrillasActividades.AsNoTracking()
                .Include(a => a.Tecnico).Include(a => a.Cuadrilla).Include(a => a.Proyecto)
                .Where(a => a.Fecha >= desde && a.Fecha <= hasta);

            if (!verTodas) query = query.Where(a => a.TecnicoId == usuarioId);
            else if (tecnicoId.HasValue) query = query.Where(a => a.TecnicoId == tecnicoId.Value);

            var actividades = await query.OrderBy(a => a.Fecha).ThenBy(a => a.Tecnico!.Nombre).ThenBy(a => a.HoraInicio).ToListAsync();

            using var wb = new XLWorkbook();
            HojaActividades(wb.AddWorksheet("Actividades"), actividades);

            if (tieneKpi)
            {
                var kpi = await _kpi.CalcularAsync(desde, hasta, verTodas ? tecnicoId : null, null, null);
                HojaKpi(wb.AddWorksheet("KPI"), kpi);
                HojaImproductivos(wb.AddWorksheet("Días improductivos"), kpi);
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var nombre = $"cuadrillas_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.xlsx";
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombre);
        }

        // ---------------------------------------------------------
        private static void HojaActividades(IXLWorksheet ws, List<CuadrillaActividad> lista)
        {
            var enc = new[]
            {
                "Fecha", "Técnico", "Cuadrilla", "Tipo", "Proyecto / sede", "Línea", "Actividad", "OT",
                "Inicio", "Fin", "H. planeadas", "H. reales", "Prioridad", "Estado", "Planificación",
                "Supervisor", "Novedad", "Detalle bloqueo", "Siguiente acción", "Observaciones"
            };
            Encabezado(ws, 1, enc);

            var r = 2;
            foreach (var a in lista)
            {
                var proyecto = a.Proyecto != null ? $"{a.Proyecto.Codigo} · {a.Proyecto.Nombre}" : a.ProyectoReferencia;
                var valores = new object?[]
                {
                    a.Fecha.Date, a.Tecnico?.Nombre, a.Cuadrilla?.Nombre, a.TipoRegistro, proyecto, a.LineaNegocio,
                    a.Actividad, a.OT, a.HoraInicio, a.HoraFin, a.HorasPlaneadas, a.HorasReales, a.Prioridad,
                    a.Estado, a.EstadoPlanificacion, a.SupervisorNombre, a.TipoNovedad, a.DetalleBloqueo,
                    a.SiguienteAccion, a.Observaciones
                };
                for (var c = 0; c < valores.Length; c++) Poner(ws, r, c + 1, valores[c]);
                ws.Cell(r, 1).Style.DateFormat.Format = "yyyy-mm-dd";
                r++;
            }
            ws.SheetView.FreezeRows(1);
            if (lista.Count > 0) ws.Range(1, 1, r - 1, enc.Length).SetAutoFilter();
            ws.Columns().AdjustToContents(1, Math.Min(r, 200));
            foreach (var col in new[] { 5, 7, 18, 19, 20 }) ws.Column(col).Width = Math.Min(ws.Column(col).Width, 45);
        }

        private static void HojaKpi(IXLWorksheet ws, CuadrillaKpiResult k)
        {
            ws.Cell(1, 1).Value = $"Indicadores del {k.Desde:yyyy-MM-dd} al {k.Hasta:yyyy-MM-dd} (corte de cálculo: {k.CorteHasta:yyyy-MM-dd})";
            ws.Cell(1, 1).Style.Font.Bold = true;

            var r = 3;
            ws.Cell(r++, 1).Value = "GLOBAL";
            ws.Cell(r - 1, 1).Style.Font.Bold = true;
            Encabezado(ws, r++, new[] { "Indicador", "Valor %", "Meta %", "Peso %", "Numerador", "Denominador", "Cumple", "Fórmula", "Frecuencia" });
            foreach (var i in k.Global.Indicadores)
            {
                var vals = new object?[] { i.Titulo, i.TieneDatos ? i.Valor : null, i.Meta, i.Peso, i.Numerador, i.Denominador,
                                           i.Cumple == null ? "N/D" : (i.Cumple == true ? "Sí" : "No"), i.Formula, i.Frecuencia };
                for (var c = 0; c < vals.Length; c++) Poner(ws, r, c + 1, vals[c]);
                r++;
            }
            Poner(ws, r, 1, "Horas productivas"); Poner(ws, r++, 2, k.Global.HorasProductivas);
            Poner(ws, r, 1, "Horas disponibles"); Poner(ws, r++, 2, k.Global.HorasDisponibles);
            Poner(ws, r, 1, "Días improductivos"); Poner(ws, r++, 2, k.Global.DiasImproductivos);
            Poner(ws, r, 1, "Días justificados"); Poner(ws, r++, 2, k.Global.DiasJustificados);

            r += 1;
            ws.Cell(r++, 1).Value = "POR TÉCNICO";
            ws.Cell(r - 1, 1).Style.Font.Bold = true;
            var titulos = k.Global.Indicadores.Select(i => i.Titulo + " %").ToList();
            var enc = new List<string> { "Técnico" };
            enc.AddRange(titulos);
            enc.AddRange(new[] { "H. productivas", "H. disponibles", "Días improd.", "Días justif." });
            Encabezado(ws, r++, enc.ToArray());
            foreach (var t in k.PorTecnico)
            {
                var vals = new List<object?> { t.TecnicoNombre };
                foreach (var i in t.Resultado.Indicadores) vals.Add(i.TieneDatos ? i.Valor : null);
                vals.AddRange(new object?[] { t.Resultado.HorasProductivas, t.Resultado.HorasDisponibles, t.Resultado.DiasImproductivos, t.Resultado.DiasJustificados });
                for (var c = 0; c < vals.Count; c++) Poner(ws, r, c + 1, vals[c]);
                r++;
            }

            if (k.Notas.Count > 0)
            {
                r += 1;
                ws.Cell(r++, 1).Value = "NOTAS";
                ws.Cell(r - 1, 1).Style.Font.Bold = true;
                foreach (var n in k.Notas) ws.Cell(r++, 1).Value = n;
            }
            ws.Columns(1, 9).AdjustToContents(1, Math.Min(r, 60));
            ws.Column(1).Width = Math.Min(ws.Column(1).Width, 45);
            ws.Column(8).Width = Math.Min(ws.Column(8).Width, 60);
        }

        private static void HojaImproductivos(IXLWorksheet ws, CuadrillaKpiResult k)
        {
            Encabezado(ws, 1, new[] { "Fecha", "Técnico", "Origen", "Motivo", "Justificado" });
            var r = 2;
            foreach (var d in k.DiasImproductivos.OrderBy(x => x.Fecha).ThenBy(x => x.TecnicoNombre))
            {
                Poner(ws, r, 1, d.Fecha.Date); ws.Cell(r, 1).Style.DateFormat.Format = "yyyy-mm-dd";
                Poner(ws, r, 2, d.TecnicoNombre);
                Poner(ws, r, 3, d.Origen);
                Poner(ws, r, 4, d.Motivo);
                Poner(ws, r, 5, d.Justificado ? "Sí" : "No");
                r++;
            }
            ws.SheetView.FreezeRows(1);
            ws.Columns().AdjustToContents(1, Math.Min(r, 200));
        }

        // ---------------------------------------------------------
        private static void Encabezado(IXLWorksheet ws, int fila, string[] titulos)
        {
            for (var c = 0; c < titulos.Length; c++)
            {
                var cell = ws.Cell(fila, c + 1);
                cell.Value = titulos[c];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
            }
        }

        private static void Poner(IXLWorksheet ws, int fila, int col, object? valor)
        {
            var cell = ws.Cell(fila, col);
            switch (valor)
            {
                case null: break;
                case string s: cell.Value = s; break;
                case DateTime d: cell.Value = d; break;
                case decimal m: cell.Value = (double)m; break;
                case double db: cell.Value = db; break;
                case int n: cell.Value = n; break;
                case bool b: cell.Value = b; break;
                default: cell.Value = valor.ToString(); break;
            }
        }
    }
}
