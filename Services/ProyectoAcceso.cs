using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Tracker;

namespace RmsErp.Api.Services
{
    /// <summary>
    /// Visibilidad de proyectos en la API (antes solo se filtraba en Angular):
    ///  - proyectos.gerente  -> ve todos los proyectos
    ///  - resto              -> solo los proyectos donde es el responsable
    /// Aplica a proyectos y a sus subregistros (OT, requisiciones, anticipos, cortes, observaciones).
    /// </summary>
    public static class ProyectoAcceso
    {
        public const string PermisoVerTodos = "proyectos.gerente";

        public static (Guid usuarioId, bool verTodos) Contexto(IHttpContextAccessor http)
        {
            var user = http.HttpContext?.User;
            var idStr = user?.FindFirst("rms_user_id")?.Value;
            var usuarioId = Guid.TryParse(idStr, out var g) ? g : Guid.Empty;
            var verTodos = user?.HasClaim("permission", PermisoVerTodos) == true;
            return (usuarioId, verTodos);
        }

        public static IQueryable<Proyecto> Visibles(IQueryable<Proyecto> q, IHttpContextAccessor http)
        {
            var (uid, verTodos) = Contexto(http);
            return verTodos ? q : q.Where(p => p.ResponsableId == uid);
        }
            // =========================================================
        // MODIFICACIONES: solo el responsable del proyecto o un gerente
        // =========================================================
        private static GraphQLException NoAutorizado(string detalle) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(detalle)
                .SetCode("PROYECTO_NO_AUTORIZADO")
                .Build());

        /// <summary>Exige ser responsable del proyecto o tener proyectos.gerente.</summary>
        public static async Task ValidarProyectoAsync(ApplicationDbContext ctx, IHttpContextAccessor http, Guid proyectoId)
        {
            var (uid, verTodos) = Contexto(http);
            if (verTodos) return;
            var proyecto = await ctx.Proyectos.AsNoTracking()
                .Where(p => p.Id == proyectoId)
                .Select(p => new { p.ResponsableId })
                .FirstOrDefaultAsync();
            if (proyecto == null) return; // la mutation informa "no encontrado"
            if (proyecto.ResponsableId != uid)
                throw NoAutorizado("Solo el responsable del proyecto o un gerente pueden realizar esta acción.");
        }

        public static async Task ValidarRequisicionAsync(ApplicationDbContext ctx, IHttpContextAccessor http, Guid id)
        {
            var pid = await ctx.ProyectoRequisiciones.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.ProyectoId).FirstOrDefaultAsync();
            if (pid.HasValue) await ValidarProyectoAsync(ctx, http, pid.Value);
        }

        public static async Task ValidarOrdenTrabajoAsync(ApplicationDbContext ctx, IHttpContextAccessor http, Guid id)
        {
            var pid = await ctx.ProyectoOrdenesTrabajo.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.ProyectoId).FirstOrDefaultAsync();
            if (pid.HasValue) await ValidarProyectoAsync(ctx, http, pid.Value);
        }

        public static async Task ValidarAnticipoAsync(ApplicationDbContext ctx, IHttpContextAccessor http, Guid id)
        {
            var pid = await ctx.ProyectoAnticipos.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.ProyectoId).FirstOrDefaultAsync();
            if (pid.HasValue) await ValidarProyectoAsync(ctx, http, pid.Value);
        }

        public static async Task ValidarAnticipoDirectoAsync(ApplicationDbContext ctx, IHttpContextAccessor http, Guid id)
        {
            var pid = await ctx.ProyectoAnticiposDirectos.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.ProyectoId).FirstOrDefaultAsync();
            if (pid.HasValue) await ValidarProyectoAsync(ctx, http, pid.Value);
        }

        public static async Task ValidarCorteAsync(ApplicationDbContext ctx, IHttpContextAccessor http, Guid id)
        {
            var pid = await ctx.ProyectoCortesFacturacion.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.ProyectoId).FirstOrDefaultAsync();
            if (pid.HasValue) await ValidarProyectoAsync(ctx, http, pid.Value);
        }

        /// <summary>Solo un gerente puede cambiar el responsable de un proyecto existente.</summary>
        public static async Task ValidarCambioResponsableAsync(ApplicationDbContext ctx, IHttpContextAccessor http, Guid proyectoId, Guid? nuevoResponsableId)
        {
            var (_, verTodos) = Contexto(http);
            if (verTodos) return;
            var actual = await ctx.Proyectos.AsNoTracking().Where(p => p.Id == proyectoId).Select(p => p.ResponsableId).FirstOrDefaultAsync();
            if (actual != nuevoResponsableId)
                throw NoAutorizado("Solo un gerente puede cambiar el responsable del proyecto.");
        }

        /// <summary>Acciones reservadas a gerencia (p. ej. eliminar definitivamente).</summary>
        public static void ValidarGerente(IHttpContextAccessor http)
        {
            var (_, verTodos) = Contexto(http);
            if (!verTodos) throw NoAutorizado("Esta acción solo la puede realizar un gerente.");
        }
    }
}
