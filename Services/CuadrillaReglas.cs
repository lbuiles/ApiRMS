using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Cuadrillas;

namespace RmsErp.Api.Services
{
    /// <summary>
    /// Reglas de negocio del módulo Cuadrillas (catálogos, horas efectivas, metas de los KPI).
    /// Fuente única: las usan las mutations (validación) y el servicio de KPI (cálculo).
    /// </summary>
    public static class CuadrillaReglas
    {
        public static readonly string[] TiposRegistro =
            { "Actividad", "VisitaSupervision", "Permiso", "Descanso", "Disponible", "Locativa" };

        public static readonly string[] EstadosEjecucion =
            { "Planeada", "En ejecución", "Finalizada", "Bloqueada", "Reprogramada" };

        public static readonly string[] Prioridades = { "Baja", "Media", "Alta", "Crítica" };

        public static readonly string[] EstadosPlanificacion =
            { "Programada", "Pendiente de confirmar", "Reprogramada", "Cancelada" };

        // Almuerzo descontado cuando el horario cruza 12:00-13:00 (regla del cliente)
        public const int AlmuerzoInicioMin = 12 * 60;
        public const int AlmuerzoFinMin = 13 * 60;

        // Metas y pesos de los indicadores (definidos por el cliente)
        public const double MetaUtilizacion = 85;
        public const double MetaCumplimiento = 90;
        public const double MetaVisitas = 95;
        public const double PesoUtilizacion = 50;
        public const double PesoCumplimiento = 25;
        public const double PesoVisitas = 25;

        private static readonly Regex RxLocativa =
            new Regex("locativ|oficina", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>Permiso, Descanso y Disponible no son trabajo operativo.</summary>
        public static bool EsNoLaboral(string tipo) =>
            tipo == "Permiso" || tipo == "Descanso" || tipo == "Disponible";

        /// <summary>Actividad operativa vigente (no es permiso/descanso/disponible y no está cancelada).</summary>
        public static bool EsOperativa(CuadrillaActividad a) =>
            !EsNoLaboral(a.TipoRegistro) && a.EstadoPlanificacion != "Cancelada";

        public static bool EsLocativa(CuadrillaActividad a) =>
            a.TipoRegistro == "Locativa" ||
            RxLocativa.IsMatch((a.Actividad ?? "") + " " + (a.Observaciones ?? ""));

        /// <summary>'HH:mm' a minutos desde las 00:00; null si el formato no es válido.</summary>
        public static int? Minutos(string? hhmm)
        {
            if (string.IsNullOrWhiteSpace(hhmm) || hhmm.Length != 5 || hhmm[2] != ':') return null;
            if (!int.TryParse(hhmm.Substring(0, 2), out var h) || !int.TryParse(hhmm.Substring(3, 2), out var m)) return null;
            if (h < 0 || h > 23 || m < 0 || m > 59) return null;
            return h * 60 + m;
        }

        public static double AlmuerzoHoras(CuadrillaActividad a)
        {
            var ini = Minutos(a.HoraInicio);
            var fin = Minutos(a.HoraFin);
            if (ini == null || fin == null || fin <= ini) return 0;
            var solape = Math.Max(0, Math.Min(fin.Value, AlmuerzoFinMin) - Math.Max(ini.Value, AlmuerzoInicioMin));
            return solape / 60.0;
        }

        /// <summary>Horas planeadas efectivas: por horario (menos almuerzo) o, si no hay horario, las horas planeadas.</summary>
        public static double HorasPlaneadasEfectivas(CuadrillaActividad a)
        {
            if (EsNoLaboral(a.TipoRegistro)) return 0;
            var ini = Minutos(a.HoraInicio);
            var fin = Minutos(a.HoraFin);
            if (ini != null && fin != null && fin > ini)
                return Math.Max(0, (fin.Value - ini.Value) / 60.0 - AlmuerzoHoras(a));
            return Math.Max(0, (double)(a.HorasPlaneadas ?? 0));
        }

        /// <summary>Horas reales efectivas: las reales registradas menos el almuerzo que cruza el horario.</summary>
        public static double HorasRealesEfectivas(CuadrillaActividad a)
        {
            if (EsNoLaboral(a.TipoRegistro) || a.HorasReales == null) return 0;
            return Math.Max(0, (double)a.HorasReales.Value - AlmuerzoHoras(a));
        }

        /// <summary>Hora de Colombia (UTC-5, sin horario de verano).</summary>
        public static DateTime HoyColombia() => DateTime.UtcNow.AddHours(-5).Date;

        // ---- Parámetros configurables (tabla Cuadrillas_Parametros) ----
        public const string ParamDiasRetroactivo = "DiasMaxRetroactivo";
        public const int DiasRetroactivoDefecto = 7;

        /// <summary>Días hacia atrás que quien programa puede crear/editar/eliminar sin permiso de configuración.</summary>
        public static async Task<int> DiasRetroactivoAsync(ApplicationDbContext context)
        {
            var valor = await context.CuadrillasParametros.AsNoTracking()
                .Where(p => p.Clave == ParamDiasRetroactivo)
                .Select(p => p.Valor)
                .FirstOrDefaultAsync();
            return int.TryParse(valor, out var n) && n >= 0 ? n : DiasRetroactivoDefecto;
        }

        /// <summary>Lanza error si la fecha es anterior a la ventana permitida y el usuario no puede configurar.</summary>
        public static async Task ValidarFechaRetroactivaAsync(ApplicationDbContext context, DateTime fecha, bool puedeConfig)
        {
            if (puedeConfig) return;
            var dias = await DiasRetroactivoAsync(context);
            var limite = HoyColombia().AddDays(-dias);
            if (fecha.Date < limite)
                throw new HotChocolate.GraphQLException(
                    $"No se pueden crear, modificar ni eliminar actividades con más de {dias} día(s) de antigüedad ({limite:yyyy-MM-dd}). " +
                    "Solicita a un administrador de Cuadrillas que realice el cambio.");
        }
    }
}
