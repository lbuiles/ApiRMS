using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
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
    }
}
