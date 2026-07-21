using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Authorization;
using System.Linq;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Tracker;

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
        public IQueryable<ProyectoRequisicion> GetRequisiciones([Service] ApplicationDbContext context)
        {
            /* * Al tener [UseFiltering], Angular podrá pedir:
             * "Tráeme las requisiciones WHERE ProyectoId == X" (Para el panel lateral)
             * O también:
             * "Tráeme las requisiciones WHERE Estado == 'Por Aprobar'" (Para el buzón del Gerente)
             */
            return context.ProyectoRequisiciones;
        }

        // ==========================================
        // 2. ÓRDENES DE TRABAJO (Contratistas)
        // ==========================================
        [Authorize(Policy = "proyectos.leer")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ProyectoOrdenTrabajo> GetOrdenesTrabajo([Service] ApplicationDbContext context)
        {
            return context.ProyectoOrdenesTrabajo;
        }

        // ==========================================
        // 3. ANTICIPOS (Financiero / Egresos)
        // ==========================================
        [Authorize(Policy = "proyectos.leer")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ProyectoAnticipo> GetAnticipos([Service] ApplicationDbContext context)
        {
            return context.ProyectoAnticipos;
        }
    }
}