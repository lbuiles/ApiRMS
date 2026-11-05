using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace RmsErp.Api.Services
{
    /// <summary>
    /// Contexto de acceso del usuario al módulo Cuadrillas, leído de los claims del token
    /// (mismo patrón que el módulo Tareas).
    ///
    ///   proyectos.cuadrillas.ver        → ve SOLO sus propias actividades
    ///   proyectos.cuadrillas.programar  → ve y gestiona TODAS las cuadrillas
    ///   proyectos.cuadrillas.kpi        → consulta indicadores
    ///   proyectos.cuadrillas.config     → configura horas, festivos, compensatorios y cuadrillas
    /// </summary>
    public static class CuadrillaAcceso
    {
        public static (Guid usuarioId, bool verTodas, bool puedeConfig) Contexto(IHttpContextAccessor http)
        {
            var user = http.HttpContext?.User;
            var idStr = user?.FindFirst("rms_user_id")?.Value;
            var usuarioId = Guid.TryParse(idStr, out var guid) ? guid : Guid.Empty;

            var verTodas = user?.HasClaim("permission", "proyectos.cuadrillas.programar") == true;
            var puedeConfig = user?.HasClaim("permission", "proyectos.cuadrillas.config") == true;

            return (usuarioId, verTodas, puedeConfig);
        }
    }
}
