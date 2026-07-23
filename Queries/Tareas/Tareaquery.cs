using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Tareas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RmsErp.Api.Queries.Tareas
{
    // DTO para el reporte semanal de cumplimiento
    public class ReporteSemanalDto
    {
        public DateTime Semana { get; set; }
        public int TotalTareas { get; set; }
        public int Completadas { get; set; }
        public int Vencidas { get; set; }
        public int Movidas { get; set; }
        public decimal PorcentajeCumplimiento { get; set; }
        public List<TareaResumenDto> TareasVencidas { get; set; } = new();
        public List<TareaResumenDto> TareasMovidasMultiple { get; set; } = new();
    }

    public class TareaResumenDto
    {
        public Guid Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;
        public string EstadoColor { get; set; } = string.Empty;
        public DateTime? FechaPresupuestoFin { get; set; }
        public int VecesMovida { get; set; }
        public List<string> Asignados { get; set; } = new();
    }

    // DTO para cumplimiento comparativo por persona
    public class TareaPersonaResumenDto
    {
        public string Id { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;
        public string EstadoColor { get; set; } = string.Empty;
        public bool EsEstadoFinal { get; set; }
        public DateTime SemanaProgramada { get; set; }
        public DateTime? FechaPresupuestoFin { get; set; }
        public DateTime? FechaRealFinalizacion { get; set; }
        public int VecesMovida { get; set; }
        public bool EsVencida { get; set; }
        /// <summary>Terminó en estado final pero después del domingo de la semana programada.</summary>
        public bool CompletadaTarde { get; set; }
        /// <summary>Días entre el domingo de la semana programada y FechaRealFinalizacion (0 si fue a tiempo).</summary>
        public int DiasRetraso { get; set; }
    }

    public class CumplimientoPersonaDto
    {
        public string UsuarioId { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int Total { get; set; }
        public int Completadas { get; set; }
        /// <summary>Completadas dentro del domingo de la semana programada.</summary>
        public int CompletadasATiempo { get; set; }
        /// <summary>Completadas después del domingo de la semana programada.</summary>
        public int CompletadasTarde { get; set; }
        public int Pendientes { get; set; }
        public int Vencidas { get; set; }
        public int Movidas { get; set; }
        /// <summary>% de tareas completadas (a tiempo + tarde) sobre el total.</summary>
        public double PorcentajeCumplimiento { get; set; }
        /// <summary>% de tareas completadas A TIEMPO sobre el total.</summary>
        public double PorcentajeATiempo { get; set; }
        /// <summary>% de tareas completadas CON RETRASO sobre el total.</summary>
        public double PorcentajeTarde { get; set; }
        public List<TareaPersonaResumenDto> Tareas { get; set; } = new();
    }

    // DTO para cumplimiento por persona dentro de un mes
    public class CumplimientoPersonaMesDto
    {
        public string UsuarioId    { get; set; } = string.Empty;
        public string Nombre       { get; set; } = string.Empty;
        public string? AvatarUrl   { get; set; }
        public int    Total        { get; set; }
        public int    Completadas  { get; set; }
        /// <summary>Completadas dentro del domingo de la semana programada.</summary>
        public int    CompletadasATiempo { get; set; }
        /// <summary>Completadas después del domingo de la semana programada.</summary>
        public int    CompletadasTarde { get; set; }
        public int    Vencidas     { get; set; }
        /// <summary>% de tareas completadas (a tiempo + tarde) sobre el total.</summary>
        public double PorcentajeCumplimiento { get; set; }
        /// <summary>% de tareas completadas A TIEMPO sobre el total.</summary>
        public double PorcentajeATiempo { get; set; }
        /// <summary>% de tareas completadas CON RETRASO sobre el total.</summary>
        public double PorcentajeTarde { get; set; }
    }

    // DTO para el reporte mensual consolidado
    public class CumplimientoMensualDto
    {
        public int    Anio         { get; set; }
        public int    Mes          { get; set; }
        public string NombreMes    { get; set; } = string.Empty;
        public int    Total        { get; set; }
        public int    Completadas  { get; set; }
        /// <summary>Completadas dentro del domingo de la semana programada.</summary>
        public int    CompletadasATiempo { get; set; }
        /// <summary>Completadas después del domingo de la semana programada.</summary>
        public int    CompletadasTarde { get; set; }
        public int    Vencidas     { get; set; }
        /// <summary>% de tareas completadas (a tiempo + tarde) sobre el total.</summary>
        public double PorcentajeCumplimiento { get; set; }
        /// <summary>% de tareas completadas A TIEMPO sobre el total.</summary>
        public double PorcentajeATiempo { get; set; }
        /// <summary>% de tareas completadas CON RETRASO sobre el total.</summary>
        public double PorcentajeTarde { get; set; }
        public List<CumplimientoPersonaMesDto> Personas { get; set; } = new();
    }

    // DTO para el reporte detallado de tareas (tabla completa)
    public class TareaDetalleReporteDto
    {
        public string Id { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public DateTime SemanaProgramada { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public string EstadoColor { get; set; } = string.Empty;
        public bool EsEstadoFinal { get; set; }
        public List<string> Responsables { get; set; } = new();
        public DateTime? FechaPresupuestoInicio { get; set; }
        public DateTime? FechaPresupuestoFin { get; set; }
        public DateTime? FechaRealFinalizacion { get; set; }
        /// <summary>Días entre FechaPresupuestoFin y FechaRealFinalizacion (positivo = tarde, negativo = adelantado).</summary>
        public int? DiasVsPresupuesto { get; set; }
        public int VecesMovida { get; set; }
        public bool EsVencida { get; set; }
        public bool CompletadaTarde { get; set; }
        public int DiasRetraso { get; set; }
        public int ComentariosCount { get; set; }
    }

    [ExtendObjectType("Query")]
    [Authorize]
    public class TareaQuery
    {
        // ── Helper: ¿la tarea se completó después del domingo de su semana programada? ──
        private static (bool Tarde, int Dias) RetrasoCompletado(DateTime semanaProgramada, DateTime? fechaRealFin, bool esEstadoFinal)
        {
            if (!esEstadoFinal || !fechaRealFin.HasValue) return (false, 0);
            var domingo = semanaProgramada.Date.AddDays(6); // lunes + 6 = domingo
            var retraso = (int)Math.Ceiling((fechaRealFin.Value.Date - domingo).TotalDays);
            return retraso > 0 ? (true, retraso) : (false, 0);
        }

        // ── Helper: porcentaje redondeado a 1 decimal ──
        private static double Pct(int parte, int total)
            => total > 0 ? Math.Round((double)parte / total * 100, 1) : 0;

        // ── Helper: extrae el usuarioId del claim "rms_user_id" y determina
        //           si el usuario puede ver tareas de todos o solo las propias.
        //
        //   Regla de visibilidad:
        //     tareas.asignar  → ve TODAS (necesita ver todas para poder asignar)
        //     tareas.reportes → ve TODAS (reportes gerenciales)
        //     solo tareas.ver → ve ÚNICAMENTE sus propias tareas asignadas
        // ──────────────────────────────────────────────────────────────────────
        private static (Guid usuarioId, bool verTodas) ObtenerContextoUsuario(IHttpContextAccessor http)
        {
            var user = http.HttpContext?.User;

            var idStr     = user?.FindFirst("rms_user_id")?.Value;
            var usuarioId = Guid.TryParse(idStr, out var guid) ? guid : Guid.Empty;

            var verTodas = user?.HasClaim("permission", "tareas.asignar")  == true
                        || user?.HasClaim("permission", "tareas.reportes") == true;

            return (usuarioId, verTodas);
        }

        // =========================================================
        // CONSULTA PRINCIPAL — Tablero Kanban
        //
        // Con tareas.asignar o tareas.reportes: devuelve TODAS las tareas.
        // Con solo tareas.ver:                  devuelve solo las del usuario.
        // =========================================================
        [Authorize(Policy = "tareas.ver")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Tarea> GetTareas(
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, verTodas) = ObtenerContextoUsuario(http);

            IQueryable<Tarea> query = context.Tareas
                .Include(t => t.Estado)
                .Include(t => t.CreadoPor)
                .Include(t => t.Asignados).ThenInclude(a => a.Usuario)
                .Include(t => t.Comentarios)
                .Include(t => t.Movimientos);

            if (!verTodas)
            {
                query = query.Where(t => t.Asignados.Any(a => a.UsuarioId == usuarioId));
            }

            return query
                .OrderBy(t => t.SemanaProgramada)
                    .ThenBy(t => t.Estado!.Orden)
                    .ThenBy(t => t.FechaPresupuestoFin);
        }

        // =========================================================
        // CONSULTA POR SEMANA — Vista semanal y reuniones
        //
        // Aplica el mismo filtro de visibilidad que GetTareas.
        // =========================================================
        [Authorize(Policy = "tareas.ver")]
        public async Task<List<Tarea>> GetTareasSemana(
            DateTime semana,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, verTodas) = ObtenerContextoUsuario(http);

            var lunes = semana.Date.AddDays(-(int)semana.DayOfWeek == 0 ? 6 : (int)semana.DayOfWeek - 1);

            IQueryable<Tarea> query = context.Tareas
                .Where(t => t.SemanaProgramada == lunes)
                .Include(t => t.Estado)
                .Include(t => t.CreadoPor)
                .Include(t => t.Asignados).ThenInclude(a => a.Usuario)
                .Include(t => t.Comentarios)
                .Include(t => t.Movimientos);

            if (!verTodas)
            {
                query = query.Where(t => t.Asignados.Any(a => a.UsuarioId == usuarioId));
            }

            return await query
                .OrderBy(t => t.Estado!.Orden)
                    .ThenBy(t => t.FechaPresupuestoFin)
                .ToListAsync();
        }

        // =========================================================
        // ESTADOS CONFIGURABLES — Columnas del Kanban
        // =========================================================
        [Authorize(Policy = "tareas.ver")]
        [UseProjection]
        [UseSorting]
        public IQueryable<EstadoTarea> GetEstadosTarea([Service] ApplicationDbContext context)
        {
            return context.TareasEstados
                .Where(e => e.Activo)
                .OrderBy(e => e.Orden);
        }

        // Incluye inactivos — solo para administración de estados
        [Authorize(Policy = "tareas.estados.admin")]
        [UseProjection]
        [UseSorting]
        public IQueryable<EstadoTarea> GetTodosEstadosTarea([Service] ApplicationDbContext context)
        {
            return context.TareasEstados.OrderBy(e => e.Orden);
        }

        // =========================================================
        // DETALLE DE UNA TAREA — Con historial completo
        //
        // Valida que el usuario tenga acceso a esta tarea específica:
        //   - Si verTodas: accede a cualquier tarea.
        //   - Si solo tareas.ver: solo si está asignado a ella.
        // =========================================================
        [Authorize(Policy = "tareas.ver")]
        public async Task<Tarea?> GetTarea(
            Guid id,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, verTodas) = ObtenerContextoUsuario(http);

            IQueryable<Tarea> query = context.Tareas
                .Include(t => t.Estado)
                .Include(t => t.CreadoPor)
                .Include(t => t.Asignados).ThenInclude(a => a.Usuario)
                .Include(t => t.Comentarios.OrderByDescending(c => c.FechaRegistro))
                    .ThenInclude(c => c.Usuario)
                .Include(t => t.Movimientos.OrderByDescending(m => m.FechaMovimiento))
                    .ThenInclude(m => m.MovidoPor)
                .Include(t => t.HistorialEstados.OrderByDescending(h => h.FechaCambio))
                    .ThenInclude(h => h.EstadoAnterior)
                .Include(t => t.HistorialEstados)
                    .ThenInclude(h => h.EstadoNuevo)
                .Include(t => t.HistorialEstados)
                    .ThenInclude(h => h.CambiadoPor)
                .Where(t => t.Id == id);

            if (!verTodas)
            {
                query = query.Where(t => t.Asignados.Any(a => a.UsuarioId == usuarioId));
            }

            return await query.FirstOrDefaultAsync();
        }

        // =========================================================
        // REPORTE SEMANAL — Para reuniones de cumplimiento
        // Solo accesible con tareas.reportes (gerencial).
        // =========================================================
        [Authorize(Policy = "tareas.reportes")]
        public async Task<ReporteSemanalDto> GetReporteSemanal(
            DateTime semana,
            [Service] ApplicationDbContext context)
        {
            var lunes = semana.Date.AddDays(-(int)semana.DayOfWeek == 0 ? 6 : (int)semana.DayOfWeek - 1);
            var hoy = DateTime.UtcNow.Date;

            var tareas = await context.Tareas
                .Where(t => t.SemanaProgramada == lunes)
                .Include(t => t.Estado)
                .Include(t => t.Asignados).ThenInclude(a => a.Usuario)
                .ToListAsync();

            var estadosFinales = await context.TareasEstados
                .Where(e => e.EsEstadoFinal)
                .Select(e => e.Id)
                .ToListAsync();

            var completadas = tareas.Count(t => estadosFinales.Contains(t.EstadoId));
            var total = tareas.Count;

            var vencidas = tareas
                .Where(t => !estadosFinales.Contains(t.EstadoId)
                         && t.FechaPresupuestoFin.HasValue
                         && t.FechaPresupuestoFin.Value.Date < hoy)
                .Select(t => new TareaResumenDto
                {
                    Id = t.Id,
                    Titulo = t.Titulo,
                    EstadoNombre = t.Estado?.Nombre ?? "",
                    EstadoColor = t.Estado?.Color ?? "",
                    FechaPresupuestoFin = t.FechaPresupuestoFin,
                    VecesMovida = t.VecesMovida,
                    Asignados = t.Asignados.Select(a => a.Usuario?.Nombre ?? "").ToList()
                }).ToList();

            var movidasMultiple = tareas
                .Where(t => t.VecesMovida > 1)
                .OrderByDescending(t => t.VecesMovida)
                .Select(t => new TareaResumenDto
                {
                    Id = t.Id,
                    Titulo = t.Titulo,
                    EstadoNombre = t.Estado?.Nombre ?? "",
                    EstadoColor = t.Estado?.Color ?? "",
                    FechaPresupuestoFin = t.FechaPresupuestoFin,
                    VecesMovida = t.VecesMovida,
                    Asignados = t.Asignados.Select(a => a.Usuario?.Nombre ?? "").ToList()
                }).ToList();

            return new ReporteSemanalDto
            {
                Semana = lunes,
                TotalTareas = total,
                Completadas = completadas,
                Vencidas = vencidas.Count,
                Movidas = tareas.Count(t => t.VecesMovida > 0),
                PorcentajeCumplimiento = total > 0
                    ? Math.Round((decimal)completadas / total * 100, 1)
                    : 0,
                TareasVencidas = vencidas,
                TareasMovidasMultiple = movidasMultiple
            };
        }

        // =========================================================
        // MIS TAREAS — Tareas asignadas al usuario autenticado
        //
        // SEGURIDAD: el usuarioId siempre se lee del claim "rms_user_id",
        // nunca se acepta como parámetro externo para evitar que un usuario
        // consulte las tareas de otra persona.
        // =========================================================
        [Authorize(Policy = "tareas.ver")]
        public async Task<List<Tarea>> GetMisTareas(
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, _) = ObtenerContextoUsuario(http);

            if (usuarioId == Guid.Empty)
                return new List<Tarea>();

            return await context.TareasAsignados
                .Where(a => a.UsuarioId == usuarioId)
                .Select(a => a.Tarea)
                .Include(t => t.Estado)
                .Include(t => t.Asignados).ThenInclude(a => a.Usuario)
                .OrderBy(t => t.SemanaProgramada)
                    .ThenBy(t => t.FechaPresupuestoFin)
                .ToListAsync();
        }

        // =========================================================
        // CUMPLIMIENTO MENSUAL — Tendencia mes a mes de un año
        // Solo accesible con tareas.reportes. Devuelve datos de TODOS
        // los usuarios (vista gerencial).
        // =========================================================
        [Authorize(Policy = "tareas.reportes")]
        public async Task<List<CumplimientoMensualDto>> GetCumplimientoMensual(
            int anio,
            [Service] ApplicationDbContext context)
        {
            var desde   = new DateTime(anio, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var hasta   = new DateTime(anio, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            var ahora   = DateTime.UtcNow;

            var tareas = await context.Tareas
                .AsNoTracking()
                .Include(t => t.Estado)
                .Include(t => t.Asignados).ThenInclude(a => a.Usuario)
                .Where(t => t.SemanaProgramada >= desde && t.SemanaProgramada <= hasta)
                .ToListAsync();

            var meses = new[] {
                "Enero","Febrero","Marzo","Abril","Mayo","Junio",
                "Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre"
            };

            var resultado = new List<CumplimientoMensualDto>();

            for (int mes = 1; mes <= 12; mes++)
            {
                var tareasMes = tareas
                    .Where(t => t.SemanaProgramada.Month == mes)
                    .ToList();

                if (!tareasMes.Any())
                {
                    resultado.Add(new CumplimientoMensualDto
                    {
                        Anio = anio, Mes = mes, NombreMes = meses[mes - 1],
                        Total = 0, Completadas = 0, CompletadasATiempo = 0,
                        CompletadasTarde = 0, Vencidas = 0,
                        PorcentajeCumplimiento = 0, PorcentajeATiempo = 0, PorcentajeTarde = 0,
                        Personas = new()
                    });
                    continue;
                }

                var total = tareasMes.Count;

                var completadasTarde  = tareasMes.Count(t =>
                    RetrasoCompletado(t.SemanaProgramada, t.FechaRealFinalizacion, t.Estado!.EsEstadoFinal).Tarde);
                var completadasTotal  = tareasMes.Count(t => t.Estado!.EsEstadoFinal);
                var completadasATiempo = completadasTotal - completadasTarde;
                var vencidas = tareasMes.Count(t =>
                    !t.Estado!.EsEstadoFinal &&
                    t.FechaPresupuestoFin.HasValue &&
                    t.FechaPresupuestoFin.Value < ahora);

                var personas = tareasMes
                    .SelectMany(t => t.Asignados.Select(a => new { a.Usuario, Tarea = t }))
                    .GroupBy(x => new { x.Usuario!.Id, x.Usuario.Nombre, x.Usuario.AvatarUrl })
                    .Select(g =>
                    {
                        var ptotal        = g.Count();
                        var ptarde        = g.Count(x =>
                            RetrasoCompletado(x.Tarea.SemanaProgramada, x.Tarea.FechaRealFinalizacion, x.Tarea.Estado!.EsEstadoFinal).Tarde);
                        var pcompletadas  = g.Count(x => x.Tarea.Estado!.EsEstadoFinal);
                        var paTiempo      = pcompletadas - ptarde;
                        var pvencidas     = g.Count(x =>
                            !x.Tarea.Estado!.EsEstadoFinal &&
                            x.Tarea.FechaPresupuestoFin.HasValue &&
                            x.Tarea.FechaPresupuestoFin.Value < ahora);

                        return new CumplimientoPersonaMesDto
                        {
                            UsuarioId              = g.Key.Id.ToString(),
                            Nombre                 = g.Key.Nombre,
                            AvatarUrl              = g.Key.AvatarUrl,
                            Total                  = ptotal,
                            Completadas            = pcompletadas,
                            CompletadasATiempo     = paTiempo,
                            CompletadasTarde       = ptarde,
                            Vencidas               = pvencidas,
                            PorcentajeCumplimiento = Pct(pcompletadas, ptotal),
                            PorcentajeATiempo      = Pct(paTiempo, ptotal),
                            PorcentajeTarde        = Pct(ptarde, ptotal),
                        };
                    })
                    .OrderBy(p => p.Nombre)
                    .ToList();

                resultado.Add(new CumplimientoMensualDto
                {
                    Anio                   = anio,
                    Mes                    = mes,
                    NombreMes              = meses[mes - 1],
                    Total                  = total,
                    Completadas            = completadasTotal,
                    CompletadasATiempo     = completadasATiempo,
                    CompletadasTarde       = completadasTarde,
                    Vencidas               = vencidas,
                    PorcentajeCumplimiento = Pct(completadasTotal, total),
                    PorcentajeATiempo      = Pct(completadasATiempo, total),
                    PorcentajeTarde        = Pct(completadasTarde, total),
                    Personas               = personas
                });
            }

            return resultado;
        }

        // =========================================================
        // DETALLE COMPLETO DE TAREAS — Tabla con todos los campos
        // Solo accesible con tareas.reportes. Devuelve todas las tareas.
        // =========================================================
        [Authorize(Policy = "tareas.reportes")]
        public async Task<List<TareaDetalleReporteDto>> GetTareasDetalle(
            DateTime desde,
            DateTime hasta,
            [Service] ApplicationDbContext context)
        {
            var hastaFin = hasta.Date.AddDays(1);
            var ahora    = DateTime.UtcNow;

            var tareas = await context.Tareas
                .AsNoTracking()
                .Include(t => t.Estado)
                .Include(t => t.Asignados).ThenInclude(a => a.Usuario)
                .Include(t => t.Comentarios)
                .Where(t => t.SemanaProgramada >= desde.Date && t.SemanaProgramada < hastaFin)
                .OrderBy(t => t.SemanaProgramada)
                    .ThenBy(t => t.FechaPresupuestoFin)
                .ToListAsync();

            return tareas.Select(t =>
            {
                var ret = RetrasoCompletado(t.SemanaProgramada, t.FechaRealFinalizacion, t.Estado!.EsEstadoFinal);

                int? diasVsPresupuesto = null;
                if (t.FechaPresupuestoFin.HasValue && t.FechaRealFinalizacion.HasValue)
                {
                    diasVsPresupuesto = (int)Math.Ceiling(
                        (t.FechaRealFinalizacion.Value.Date - t.FechaPresupuestoFin.Value.Date).TotalDays);
                }

                return new TareaDetalleReporteDto
                {
                    Id                     = t.Id.ToString(),
                    Titulo                 = t.Titulo,
                    Descripcion            = t.Descripcion,
                    SemanaProgramada       = t.SemanaProgramada,
                    EstadoNombre           = t.Estado!.Nombre,
                    EstadoColor            = t.Estado.Color,
                    EsEstadoFinal          = t.Estado.EsEstadoFinal,
                    Responsables           = t.Asignados.Select(a => a.Usuario?.Nombre ?? "").ToList(),
                    FechaPresupuestoInicio = t.FechaPresupuestoInicio,
                    FechaPresupuestoFin    = t.FechaPresupuestoFin,
                    FechaRealFinalizacion  = t.FechaRealFinalizacion,
                    DiasVsPresupuesto      = diasVsPresupuesto,
                    VecesMovida            = t.VecesMovida,
                    EsVencida              = !t.Estado.EsEstadoFinal &&
                                             t.FechaPresupuestoFin.HasValue &&
                                             t.FechaPresupuestoFin.Value < ahora,
                    CompletadaTarde        = ret.Tarde,
                    DiasRetraso            = ret.Dias,
                    ComentariosCount       = t.Comentarios.Count
                };
            }).ToList();
        }

        // =========================================================
        // CUMPLIMIENTO POR PERSONA — Vista comparativa por rango de fechas
        // Solo accesible con tareas.reportes. Devuelve datos de TODOS.
        // =========================================================
        [Authorize(Policy = "tareas.reportes")]
        public async Task<List<CumplimientoPersonaDto>> GetCumplimientoPersonas(
            DateTime desde,
            DateTime hasta,
            [Service] ApplicationDbContext context)
        {
            var hastaFin = hasta.Date.AddDays(1);
            var ahora    = DateTime.UtcNow;

            var tareas = await context.Tareas
                .AsNoTracking()
                .Include(t => t.Estado)
                .Include(t => t.Asignados)
                    .ThenInclude(a => a.Usuario)
                .Where(t => t.SemanaProgramada >= desde.Date && t.SemanaProgramada < hastaFin)
                .ToListAsync();

            var resultado = tareas
                .SelectMany(t => t.Asignados.Select(a => new { a.Usuario, Tarea = t }))
                .GroupBy(x => new { x.Usuario!.Id, x.Usuario.Nombre, x.Usuario.AvatarUrl })
                .Select(g =>
                {
                    var total       = g.Count();
                    var completadas = g.Count(x => x.Tarea.Estado!.EsEstadoFinal);
                    var vencidas    = g.Count(x =>
                        !x.Tarea.Estado!.EsEstadoFinal &&
                        x.Tarea.FechaPresupuestoFin.HasValue &&
                        x.Tarea.FechaPresupuestoFin.Value < ahora);

                    var tareasList = g
                        .Select(x =>
                        {
                            var ret = RetrasoCompletado(
                                x.Tarea.SemanaProgramada,
                                x.Tarea.FechaRealFinalizacion,
                                x.Tarea.Estado!.EsEstadoFinal);
                            return new TareaPersonaResumenDto
                            {
                                Id                    = x.Tarea.Id.ToString(),
                                Titulo                = x.Tarea.Titulo,
                                EstadoNombre          = x.Tarea.Estado!.Nombre,
                                EstadoColor           = x.Tarea.Estado.Color,
                                EsEstadoFinal         = x.Tarea.Estado.EsEstadoFinal,
                                SemanaProgramada      = x.Tarea.SemanaProgramada,
                                FechaPresupuestoFin   = x.Tarea.FechaPresupuestoFin,
                                FechaRealFinalizacion = x.Tarea.FechaRealFinalizacion,
                                VecesMovida           = x.Tarea.VecesMovida,
                                EsVencida             = !x.Tarea.Estado.EsEstadoFinal &&
                                                        x.Tarea.FechaPresupuestoFin.HasValue &&
                                                        x.Tarea.FechaPresupuestoFin.Value < ahora,
                                CompletadaTarde       = ret.Tarde,
                                DiasRetraso           = ret.Dias
                            };
                        })
                        .OrderBy(x => x.EsEstadoFinal)
                        .ThenBy(x => x.Titulo)
                        .ToList();

                    var tarde    = tareasList.Count(t => t.CompletadaTarde);
                    var aTiempo  = completadas - tarde;

                    return new CumplimientoPersonaDto
                    {
                        UsuarioId              = g.Key.Id.ToString(),
                        Nombre                 = g.Key.Nombre,
                        AvatarUrl              = g.Key.AvatarUrl,
                        Total                  = total,
                        Completadas            = completadas,
                        CompletadasATiempo     = aTiempo,
                        CompletadasTarde       = tarde,
                        Pendientes             = total - completadas,
                        Vencidas               = vencidas,
                        Movidas                = g.Sum(x => x.Tarea.VecesMovida),
                        PorcentajeCumplimiento = Pct(completadas, total),
                        PorcentajeATiempo      = Pct(aTiempo, total),
                        PorcentajeTarde        = Pct(tarde, total),
                        Tareas                 = tareasList
                    };
                })
                .OrderByDescending(x => x.PorcentajeCumplimiento)
                .ToList();

            return resultado;
        }
    }
}