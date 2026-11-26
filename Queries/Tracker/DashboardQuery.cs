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

    [ExtendObjectType("Query")]
    [Authorize]
    public class DashboardQuery
    {
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