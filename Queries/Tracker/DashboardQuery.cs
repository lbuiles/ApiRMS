using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using System.Threading.Tasks;

namespace RmsErp.Api.Queries.Tracker
{
    // DTO: La estructura que le enviaremos a Angular
    public record DashboardResumen(
        int ProyectosActivos,
        decimal TotalFacturado,
        decimal TotalGastos,
        decimal UtilidadProyectada,
        int RequisicionesPendientes,
        int AnticiposPendientes
    );

    /// <summary>Movimientos con fecha de un mes (yyyy-MM): lo facturado y lo gastado en ese mes.</summary>
    public record FlujoMes(string Mes, decimal Facturado, decimal Gasto);

        [ExtendObjectType("Query")]
    [Authorize]
    public class DashboardQuery
    {
        /// <summary>
        /// Flujo de caja mes a mes del dashboard, con las mismas reglas de visibilidad y filtros:
        ///  - Facturado: cortes de facturación no anulados, por FechaCorte (Proyecto.ValorFacturado es la suma de estos cortes).
        ///  - Gasto: requisiciones compradas (ValorRealCompra) por FechaCompra.
        /// Lo registrado a mano en el proyecto sin movimiento con fecha no aparece aquí; el front muestra esa diferencia.
        /// </summary>
        [Authorize(Policy = "proyectos.leer")]
        public async Task<List<FlujoMes>> GetFlujoMensual(
            string? linea,
            string? estado,
            [Service] ApplicationDbContext context,
            [Service] Microsoft.AspNetCore.Http.IHttpContextAccessor http)
        {
            var proyectos = RmsErp.Api.Services.ProyectoAcceso.Visibles(context.Proyectos, http)
                .Where(p => !p.Archivado);
            if (!string.IsNullOrWhiteSpace(linea) && linea != "todas") proyectos = proyectos.Where(p => p.LineaNegocio == linea);
            if (!string.IsNullOrWhiteSpace(estado) && estado != "todos") proyectos = proyectos.Where(p => p.Estado == estado);
            var ids = proyectos.Select(p => p.Id);

            var facturado = await context.ProyectoCortesFacturacion
                .Where(c => ids.Contains(c.ProyectoId) && c.Estado != "ANULADO")
                .GroupBy(c => new { c.FechaCorte.Year, c.FechaCorte.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(c => c.ValorCorte) })
                .ToListAsync();

            var gasto = await context.ProyectoRequisiciones
                .Where(r => ids.Contains(r.ProyectoId) && r.FechaCompra != null && r.ValorRealCompra != null && r.Estado != "Rechazada")
                .GroupBy(r => new { r.FechaCompra!.Value.Year, r.FechaCompra!.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(r => r.ValorRealCompra ?? 0) })
                .ToListAsync();

            return facturado.Select(f => (f.Year, f.Month)).Union(gasto.Select(g => (g.Year, g.Month)))
                .Distinct()
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .Select(m => new FlujoMes(
                    $"{m.Year:D4}-{m.Month:D2}",
                    facturado.Where(f => f.Year == m.Year && f.Month == m.Month).Sum(f => f.Total),
                    gasto.Where(g => g.Year == m.Year && g.Month == m.Month).Sum(g => g.Total)))
                .ToList();
        }

        public async Task<DashboardResumen> GetDashboardResumen(
            [Service] ApplicationDbContext context,
            [Service] Microsoft.AspNetCore.Http.IHttpContextAccessor http)
        {
            // Seguridad: los totales solo incluyen los proyectos que el usuario puede ver
            var (uid, verTodos) = RmsErp.Api.Services.ProyectoAcceso.Contexto(http);
            var proyectos = RmsErp.Api.Services.ProyectoAcceso.Visibles(context.Proyectos, http);

            // 1. Proyectos Activos (Excluyendo los cerrados o cancelados)
            var proyectosActivos = await proyectos
                .CountAsync(p => p.Estado != "FINALIZADO_TOTAL" && p.Estado != "Cancelado" && p.Estado != "Incumplimiento");

            // 2. Resumen Financiero Global (Sumatoria de todos los proyectos)
            var totalFacturado = await proyectos.SumAsync(p => p.ValorFacturado);
            var totalGastos = await proyectos.SumAsync(p => p.ValorGasto);
            var utilidadProyectada = totalFacturado - totalGastos;

            // 3. Cuellos de Botella (Lo que está esperando firmas)
            var reqPendientes = await context.ProyectoRequisiciones
                .Where(r => verTodos || (r.Proyecto != null && r.Proyecto.ResponsableId == uid))
                .CountAsync(r => r.Estado == "Por Aprobar Supervisor" || r.Estado == "Por Aprobar Gerencia");
            
            var antPendientes = await context.ProyectoAnticipos
                .Where(a => verTodos || (a.Proyecto != null && a.Proyecto.ResponsableId == uid))
                .CountAsync(a => a.Estado == "SOLICITADO" || a.Estado == "Por Aprobar");

            return new DashboardResumen(
                proyectosActivos, 
                totalFacturado, 
                totalGastos, 
                utilidadProyectada,
                reqPendientes, 
                antPendientes
            );
        }
    }
}