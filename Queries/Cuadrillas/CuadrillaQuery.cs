using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Cuadrillas;
using RmsErp.Api.Services;

namespace RmsErp.Api.Queries.Cuadrillas
{
    // =========================================================
    // DTOs
    // =========================================================
    public class CuadrillaTecnicoDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public List<string> Cuadrillas { get; set; } = new List<string>();
    }

    public class CuadrillaProyectoOpcionDto
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string LineaNegocio { get; set; } = string.Empty;
        public Guid ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string? SupervisorCliente { get; set; }
    }

    public class CuadrillaSupervisorOpcionDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Cargo { get; set; }
        // CLIENTE | INTERNO
        public string Tipo { get; set; } = string.Empty;
    }

    public class CuadrillaCopiaResultadoDto
    {
        public int Copiadas { get; set; }
        public int Omitidas { get; set; }
        public string? Detalle { get; set; }
    }

    public class CuadrillaParametrosDto
    {
        public CuadrillaParametrosDto(int diasMaxRetroactivo) { DiasMaxRetroactivo = diasMaxRetroactivo; }
        public int DiasMaxRetroactivo { get; set; }
    }

    public class CuadrillaCompensatorioDto
    {
        public Guid TecnicoId { get; set; }
        public string TecnicoNombre { get; set; } = string.Empty;
        public double DiasAcumulados { get; set; }
        public int DiasGastados { get; set; }
        public double DiasPendientes { get; set; }
    }

    // =========================================================
    // QUERIES
    // =========================================================
    [ExtendObjectType("Query")]
    [Authorize]
    public class CuadrillaQuery
    {
        // ---------------------------------------------------------
        // Agenda: actividades en un rango de fechas.
        // 'programar' ve todas; 'ver' ve únicamente las propias.
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.ver")]
        public async Task<List<CuadrillaActividad>> GetCuadrillaActividades(
            DateTime desde,
            DateTime hasta,
            Guid? tecnicoId,
            Guid? proyectoId,
            string? estado,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, verTodas, _) = CuadrillaAcceso.Contexto(http);

            desde = desde.Date;
            hasta = hasta.Date;
            if (hasta < desde) throw new GraphQLException("El rango de fechas no es válido.");
            if ((hasta - desde).TotalDays > 400) throw new GraphQLException("El rango máximo de consulta es de 400 días.");

            IQueryable<CuadrillaActividad> query = context.CuadrillasActividades
                .AsNoTracking()
                .Include(a => a.Tecnico)
                .Include(a => a.Cuadrilla)
                .Include(a => a.Proyecto)
                .Include(a => a.SupervisorContacto)
                .Include(a => a.SupervisorUsuario)
                .Where(a => a.Fecha >= desde && a.Fecha <= hasta);

            if (!verTodas)
                query = query.Where(a => a.TecnicoId == usuarioId);
            else if (tecnicoId.HasValue)
                query = query.Where(a => a.TecnicoId == tecnicoId.Value);

            if (proyectoId.HasValue)
                query = query.Where(a => a.ProyectoId == proyectoId.Value);

            if (!string.IsNullOrWhiteSpace(estado))
                query = query.Where(a => a.Estado == estado);

            return await query
                .OrderBy(a => a.Fecha)
                .ThenBy(a => a.HoraInicio)
                .ToListAsync();
        }

        // ---------------------------------------------------------
        // Detalle de una actividad con su historial
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.ver")]
        public async Task<CuadrillaActividad?> GetCuadrillaActividad(
            Guid id,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, verTodas, _) = CuadrillaAcceso.Contexto(http);

            var actividad = await context.CuadrillasActividades
                .AsNoTracking()
                .Include(a => a.Tecnico)
                .Include(a => a.Cuadrilla)
                .Include(a => a.Proyecto)
                .Include(a => a.SupervisorContacto)
                .Include(a => a.SupervisorUsuario)
                .Include(a => a.Historial).ThenInclude(h => h.Usuario)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (actividad == null) return null;
            if (!verTodas && actividad.TecnicoId != usuarioId) return null;

            actividad.Historial = actividad.Historial.OrderByDescending(h => h.Fecha).ToList();
            return actividad;
        }

        // ---------------------------------------------------------
        // Técnicos (miembros de cuadrillas activas). Sin 'programar' devuelve solo al propio usuario.
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.ver")]
        public async Task<List<CuadrillaTecnicoDto>> GetCuadrillaTecnicos(
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, verTodas, _) = CuadrillaAcceso.Contexto(http);

            var query = context.CuadrillasMiembros
                .AsNoTracking()
                .Where(m => m.Cuadrilla != null && m.Cuadrilla.Activa && m.Usuario != null && m.Usuario.Estado == "ACTIVO");

            if (!verTodas) query = query.Where(m => m.UsuarioId == usuarioId);

            var filas = await query
                .Select(m => new { m.UsuarioId, Nombre = m.Usuario!.Nombre, Cuadrilla = m.Cuadrilla!.Nombre })
                .ToListAsync();

            return filas
                .GroupBy(f => new { f.UsuarioId, f.Nombre })
                .Select(g => new CuadrillaTecnicoDto
                {
                    Id = g.Key.UsuarioId,
                    Nombre = g.Key.Nombre,
                    Cuadrillas = g.Select(x => x.Cuadrilla).Distinct().ToList()
                })
                .OrderBy(t => t.Nombre)
                .ToList();
        }

        // ---------------------------------------------------------
        // Cuadrillas con sus miembros (para programar / configurar)
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<List<Cuadrilla>> GetCuadrillas([Service] ApplicationDbContext context)
        {
            return await context.Cuadrillas
                .AsNoTracking()
                .Include(c => c.Miembros).ThenInclude(m => m.Usuario)
                .OrderBy(c => c.Nombre)
                .ToListAsync();
        }

        // ---------------------------------------------------------
        // Proyectos del tracker disponibles para programar (sin exigir 'proyectos.leer')
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<List<CuadrillaProyectoOpcionDto>> GetCuadrillaProyectosDisponibles(
            [Service] ApplicationDbContext context)
        {
            // TODO (al unir la rama Ajustes-tracker-jaime): agregar && !p.Archivado
            return await context.Proyectos
                .AsNoTracking()
                .Where(p => p.Estado != "FINALIZADO_TOTAL" && p.Estado != "Cancelado" && p.Estado != "Incumplimiento")
                .OrderBy(p => p.Codigo)
                .Select(p => new CuadrillaProyectoOpcionDto
                {
                    Id = p.Id,
                    Codigo = p.Codigo,
                    Nombre = p.Nombre,
                    LineaNegocio = p.LineaNegocio,
                    ClienteId = p.ClienteId,
                    ClienteNombre = p.Cliente != null ? p.Cliente.RazonSocial : string.Empty,
                    SupervisorCliente = p.SupervisorCliente
                })
                .ToListAsync();
        }

        // ---------------------------------------------------------
        // Supervisores: contactos del cliente del proyecto, o usuarios internos
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<List<CuadrillaSupervisorOpcionDto>> GetCuadrillaSupervisoresCliente(
            Guid proyectoId,
            [Service] ApplicationDbContext context)
        {
            var clienteId = await context.Proyectos
                .Where(p => p.Id == proyectoId)
                .Select(p => (Guid?)p.ClienteId)
                .FirstOrDefaultAsync();

            if (clienteId == null) return new List<CuadrillaSupervisorOpcionDto>();

            return await context.ClientesContactos
                .AsNoTracking()
                .Where(c => c.Sucursal != null && c.Sucursal.ClienteId == clienteId.Value)
                .OrderBy(c => c.Nombre)
                .Select(c => new CuadrillaSupervisorOpcionDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Cargo = c.Cargo,
                    Tipo = "CLIENTE"
                })
                .ToListAsync();
        }

        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<List<CuadrillaSupervisorOpcionDto>> GetCuadrillaSupervisoresInternos(
            [Service] ApplicationDbContext context)
        {
            return await context.Usuarios
                .AsNoTracking()
                .Where(u => u.Estado == "ACTIVO")
                .OrderBy(u => u.Nombre)
                .Select(u => new CuadrillaSupervisorOpcionDto
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Cargo = null,
                    Tipo = "INTERNO"
                })
                .ToListAsync();
        }

        // ---------------------------------------------------------
        // Indicadores KPI (cálculo centralizado en CuadrillaKpiService)
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.kpi")]
        public async Task<CuadrillaKpiResult> GetCuadrillaKpis(
            DateTime desde,
            DateTime hasta,
            Guid? tecnicoId,
            Guid? cuadrillaId,
            Guid? proyectoId,
            [Service] CuadrillaKpiService kpi)
        {
            return await kpi.CalcularAsync(desde, hasta, tecnicoId, cuadrillaId, proyectoId);
        }

        // ---------------------------------------------------------
        // Días compensatorios: acumulados (manual) - gastados (desde la agenda)
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.ver")]
        public async Task<List<CuadrillaCompensatorioDto>> GetCuadrillaCompensatorios(
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, verTodas, _) = CuadrillaAcceso.Contexto(http);

            List<Guid> ids;
            if (verTodas)
            {
                var miembros = await context.CuadrillasMiembros
                    .Where(m => m.Cuadrilla != null && m.Cuadrilla.Activa)
                    .Select(m => m.UsuarioId).Distinct().ToListAsync();
                var conSaldo = await context.CuadrillasCompensatorios.Select(c => c.TecnicoId).ToListAsync();
                ids = miembros.Union(conSaldo).ToList();
            }
            else
            {
                ids = new List<Guid> { usuarioId };
            }

            var nombres = await context.Usuarios
                .Where(u => ids.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Nombre);

            var acumulados = await context.CuadrillasCompensatorios
                .Where(c => ids.Contains(c.TecnicoId))
                .ToDictionaryAsync(c => c.TecnicoId, c => (double)c.DiasAcumulados);

            var diasDescanso = await context.CuadrillasActividades
                .Where(a => ids.Contains(a.TecnicoId) && a.TipoRegistro == "Descanso" && a.EstadoPlanificacion != "Cancelada")
                .Select(a => new { a.TecnicoId, a.Fecha })
                .Distinct()
                .ToListAsync();

            var gastadosPorTecnico = diasDescanso
                .GroupBy(x => x.TecnicoId)
                .ToDictionary(g => g.Key, g => g.Count());

            return ids
                .Select(id =>
                {
                    var acum = acumulados.TryGetValue(id, out var a) ? a : 0;
                    var gast = gastadosPorTecnico.TryGetValue(id, out var g) ? g : 0;
                    return new CuadrillaCompensatorioDto
                    {
                        TecnicoId = id,
                        TecnicoNombre = nombres.TryGetValue(id, out var n) ? n : "(técnico)",
                        DiasAcumulados = acum,
                        DiasGastados = gast,
                        DiasPendientes = acum - gast
                    };
                })
                .OrderBy(x => x.TecnicoNombre)
                .ToList();
        }

        // ---------------------------------------------------------
        // Configuración de horas, festivos y días improductivos manuales
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.ver")]
        public async Task<CuadrillaParametrosDto> GetCuadrillaParametros([Service] ApplicationDbContext context)
        {
            return new CuadrillaParametrosDto(await CuadrillaReglas.DiasRetroactivoAsync(context));
        }

        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<List<CuadrillaConfigHoras>> GetCuadrillaConfigHoras([Service] ApplicationDbContext context)
        {
            return await context.CuadrillasConfigHoras
                .AsNoTracking()
                .Include(c => c.Tecnico)
                .OrderBy(c => c.TecnicoId)
                .ThenByDescending(c => c.VigenteDesde)
                .ToListAsync();
        }

        [Authorize(Policy = "proyectos.cuadrillas.ver")]
        public async Task<List<CuadrillaFestivo>> GetCuadrillaFestivos(
            int anio,
            [Service] ApplicationDbContext context)
        {
            var ini = new DateTime(anio, 1, 1);
            var fin = new DateTime(anio, 12, 31);
            return await context.CuadrillasFestivos
                .AsNoTracking()
                .Where(f => f.Fecha >= ini && f.Fecha <= fin)
                .OrderBy(f => f.Fecha)
                .ToListAsync();
        }

        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<List<CuadrillaDiaImproductivo>> GetCuadrillaDiasImproductivos(
            DateTime desde,
            DateTime hasta,
            Guid? tecnicoId,
            [Service] ApplicationDbContext context)
        {
            desde = desde.Date;
            hasta = hasta.Date;

            var query = context.CuadrillasDiasImproductivos
                .AsNoTracking()
                .Include(d => d.Tecnico)
                .Where(d => d.Fecha >= desde && d.Fecha <= hasta);

            if (tecnicoId.HasValue) query = query.Where(d => d.TecnicoId == tecnicoId.Value);

            return await query.OrderBy(d => d.Fecha).ThenBy(d => d.TecnicoId).ToListAsync();
        }
    }
}
