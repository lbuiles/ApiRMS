using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Authorization;
using System.Linq;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Tracker;
using Microsoft.AspNetCore.Http;
using RmsErp.Api.Services;

namespace RmsErp.Api.Queries.Tracker
{
    [ExtendObjectType("Query")]
    [Authorize]
    public class SubFlujosQuery
    {
        // ==========================================
        // 1. REQUISICIONES (Compras / Materiales)
        // ==========================================
        [Authorize(Policy = "proyectos.leer")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ProyectoRequisicion> GetRequisiciones([Service] ApplicationDbContext context, [Service] IHttpContextAccessor http)
        {
            /* * Al tener [UseFiltering], Angular podrá pedir:
             * "Tráeme las requisiciones WHERE ProyectoId == X" (Para el panel lateral)
             * O también:
             * "Tráeme las requisiciones WHERE Estado == 'Por Aprobar'" (Para el buzón del Gerente)
             */
            // Seguridad: solo registros de proyectos visibles para el usuario
            var (uid, verTodos) = ProyectoAcceso.Contexto(http);
            return context.ProyectoRequisiciones.Where(x => verTodos || (x.Proyecto != null && x.Proyecto.ResponsableId == uid));
        }

        // ==========================================
        // 2. ÓRDENES DE TRABAJO (Contratistas)
        // ==========================================
        [Authorize(Policy = "proyectos.leer")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ProyectoOrdenTrabajo> GetOrdenesTrabajo([Service] ApplicationDbContext context, [Service] IHttpContextAccessor http)
        {
            // Seguridad: solo registros de proyectos visibles para el usuario
            var (uid, verTodos) = ProyectoAcceso.Contexto(http);
            return context.ProyectoOrdenesTrabajo.Where(x => verTodos || (x.Proyecto != null && x.Proyecto.ResponsableId == uid));
        }

        // ==========================================
        // 3. ANTICIPOS (Financiero / Egresos)
        // ==========================================
        [Authorize(Policy = "proyectos.leer")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ProyectoAnticipo> GetAnticipos([Service] ApplicationDbContext context, [Service] IHttpContextAccessor http)
        {
            // Seguridad: solo registros de proyectos visibles para el usuario
            var (uid, verTodos) = ProyectoAcceso.Contexto(http);
            return context.ProyectoAnticipos.Where(x => verTodos || (x.Proyecto != null && x.Proyecto.ResponsableId == uid));
        }

        // ==========================================
        // 4. CORTES DE FACTURACIÓN (Financiero / Ingresos)
        // ==========================================
        [Authorize(Policy = "proyectos.leer")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ProyectoCorteFacturacion> GetCortesFacturacion([Service] ApplicationDbContext context, [Service] IHttpContextAccessor http)
        {
            // Seguridad: solo registros de proyectos visibles para el usuario
            var (uid, verTodos) = ProyectoAcceso.Contexto(http);
            return context.ProyectoCortesFacturacion.Where(x => verTodos || (x.Proyecto != null && x.Proyecto.ResponsableId == uid));
        }
    }
}