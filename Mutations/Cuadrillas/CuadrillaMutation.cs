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
using RmsErp.Api.Queries.Cuadrillas;
using RmsErp.Api.Services;

namespace RmsErp.Api.Mutations.Cuadrillas
{
    // =========================================================
    // INPUTS
    // =========================================================

    /// <summary>Datos de PLANIFICACIÓN de una actividad (los edita quien programa).</summary>
    public record CuadrillaActividadInput(
        DateTime Fecha,
        Guid TecnicoId,
        Guid? CuadrillaId,
        Guid? ProyectoId,
        string? ProyectoReferencia,
        string? LineaNegocio,
        string TipoRegistro,
        string? Actividad,
        string? OT,
        string? HoraInicio,
        string? HoraFin,
        decimal? HorasPlaneadas,
        string? Prioridad,
        string? Estado,
        string? EstadoPlanificacion,
        string? SupervisorTipo,          // CLIENTE | INTERNO | null
        Guid? SupervisorContactoId,
        Guid? SupervisorUsuarioId,
        string? SupervisorNombre,
        string? Observaciones,
        bool? PermitirCruce              // true = guardar aunque haya cruce horario
    );

    /// <summary>Datos de EJECUCIÓN (los actualiza el técnico dueño de la actividad o quien programa).</summary>
    public record CuadrillaEjecucionInput(
        string Estado,
        decimal? HorasReales,
        string? TipoNovedad,
        string? DetalleBloqueo,
        string? SiguienteAccion,
        string? Observaciones,
        DateTime? FechaCierre
    );

    public record CuadrillaConfigHorasInput(
        Guid? TecnicoId,                 // null = base general
        decimal HorasSemanales,
        int DiasLaborales,               // 5 | 6
        DateTime VigenteDesde
    );

    // =========================================================
    // MUTATIONS
    // =========================================================
    [ExtendObjectType("Mutation")]
    [Authorize]
    public class CuadrillaMutation
    {
        private const int MaxLote = 300;

        private static readonly string[] EstadosQuePuedeUsarTecnico =
            { "Planeada", "En ejecución", "Finalizada", "Bloqueada" };

        // ---------------------------------------------------------
        // CREAR ACTIVIDAD (programar)
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<CuadrillaActividad> CrearCuadrillaActividad(
            CuadrillaActividadInput input,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, _, puedeConfig) = CuadrillaAcceso.Contexto(http);
            ValidarUsuarioActual(usuarioId);
            await CuadrillaReglas.ValidarFechaRetroactivaAsync(context, input.Fecha, puedeConfig);

            var actividad = new CuadrillaActividad
            {
                Id = Guid.NewGuid(),
                CreadoPorId = usuarioId,
                FechaCreacion = DateTime.UtcNow
            };

            await AplicarPlanificacionAsync(actividad, input, context);
            await VerificarCruceAsync(actividad, input.PermitirCruce == true, context);

            if (actividad.Estado == "Finalizada" && actividad.FechaCierre == null)
                actividad.FechaCierre = CuadrillaReglas.HoyColombia();

            context.CuadrillasActividades.Add(actividad);
            context.CuadrillasActividadHistorial.Add(NuevoHistorial(actividad.Id, usuarioId, "Actividad creada"));
            await context.SaveChangesAsync();

            return await CargarAsync(context, actividad.Id);
        }

        // ---------------------------------------------------------
        // CREAR EN LOTE: varios técnicos y/o varios días (programar)
        //   Todo o nada: si algo falla (cruce, fecha fuera de ventana, dato inválido) no se crea nada.
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<int> CrearCuadrillaActividadesLote(
            CuadrillaActividadInput input,
            List<Guid> tecnicoIds,
            List<DateTime> fechas,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, _, puedeConfig) = CuadrillaAcceso.Contexto(http);
            ValidarUsuarioActual(usuarioId);

            var tecnicos = (tecnicoIds ?? new List<Guid>()).Distinct().ToList();
            var dias = (fechas ?? new List<DateTime>()).Select(f => f.Date).Distinct().OrderBy(f => f).ToList();
            if (tecnicos.Count == 0) throw new GraphQLException("Selecciona al menos un técnico.");
            if (dias.Count == 0) throw new GraphQLException("Selecciona al menos una fecha.");
            if (tecnicos.Count * dias.Count > MaxLote)
                throw new GraphQLException($"Máximo {MaxLote} actividades por operación (seleccionaste {tecnicos.Count * dias.Count}).");

            foreach (var f in dias)
                await CuadrillaReglas.ValidarFechaRetroactivaAsync(context, f, puedeConfig);

            var nombres = await context.Usuarios.AsNoTracking()
                .Where(u => tecnicos.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Nombre);

            var nuevas = new List<CuadrillaActividad>();
            var cruces = new List<string>();

            foreach (var tecnicoId in tecnicos)
            foreach (var fecha in dias)
            {
                var actividad = new CuadrillaActividad
                {
                    Id = Guid.NewGuid(),
                    CreadoPorId = usuarioId,
                    FechaCreacion = DateTime.UtcNow
                };
                await AplicarPlanificacionAsync(actividad, input with { Fecha = fecha, TecnicoId = tecnicoId }, context);

                try { await VerificarCruceAsync(actividad, input.PermitirCruce == true, context); }
                catch (GraphQLException ex) when (ex.Errors.Any(e => e.Code == "CRUCE_HORARIO"))
                {
                    nombres.TryGetValue(tecnicoId, out var n);
                    cruces.Add($"{n ?? "Técnico"} {fecha:yyyy-MM-dd}");
                    continue;
                }

                if (actividad.Estado == "Finalizada" && actividad.FechaCierre == null)
                    actividad.FechaCierre = CuadrillaReglas.HoyColombia();
                nuevas.Add(actividad);
            }

            if (cruces.Count > 0)
                throw CruceLote(cruces);

            context.CuadrillasActividades.AddRange(nuevas);
            context.CuadrillasActividadHistorial.AddRange(
                nuevas.Select(a => NuevoHistorial(a.Id, usuarioId, "Actividad creada (lote)")));
            await context.SaveChangesAsync();
            return nuevas.Count;
        }

        // ---------------------------------------------------------
        // COPIAR SEMANA (programar)
        //   Copia la programación de la semana origen (lunes–domingo) a la semana destino,
        //   conservando el día de la semana. Reinicia la ejecución (Planeada, sin horas reales).
        //   Omite registros ya existentes en destino (mismo técnico, día, horario y actividad).
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<CuadrillaCopiaResultadoDto> CopiarCuadrillaSemana(
            DateTime semanaOrigen,
            DateTime semanaDestino,
            List<Guid>? tecnicoIds,
            bool? permitirCruce,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, _, puedeConfig) = CuadrillaAcceso.Contexto(http);
            ValidarUsuarioActual(usuarioId);

            var origen = LunesDe(semanaOrigen);
            var destino = LunesDe(semanaDestino);
            if (origen == destino)
                throw new GraphQLException("La semana de origen y la de destino son la misma.");

            // La semana destino completa debe estar dentro de la ventana permitida
            await CuadrillaReglas.ValidarFechaRetroactivaAsync(context, destino.AddDays(6), puedeConfig);

            var origenFin = origen.AddDays(6);
            var query = context.CuadrillasActividades.AsNoTracking()
                .Where(a => a.Fecha >= origen && a.Fecha <= origenFin && a.EstadoPlanificacion != "Cancelada");
            if (tecnicoIds != null && tecnicoIds.Count > 0)
                query = query.Where(a => tecnicoIds.Contains(a.TecnicoId));
            var fuente = await query.ToListAsync();
            if (fuente.Count == 0)
                throw new GraphQLException("La semana de origen no tiene actividades para copiar.");

            var activos = await context.Usuarios.AsNoTracking()
                .Where(u => u.Estado == "ACTIVO").Select(u => u.Id).ToListAsync();
            var activosSet = activos.ToHashSet();

            var destinoFin = destino.AddDays(6);
            var existentes = await context.CuadrillasActividades.AsNoTracking()
                .Where(a => a.Fecha >= destino && a.Fecha <= destinoFin)
                .Select(a => new { a.TecnicoId, a.Fecha, a.HoraInicio, a.HoraFin, a.Actividad })
                .ToListAsync();
            var claves = existentes
                .Select(e => $"{e.TecnicoId}|{e.Fecha:yyyyMMdd}|{e.HoraInicio}|{e.HoraFin}|{e.Actividad}")
                .ToHashSet();

            var idsFuente = fuente.Select(f => f.TecnicoId).Distinct().ToList();
            var nombres = await context.Usuarios.AsNoTracking()
                .Where(u => idsFuente.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Nombre);

            var nuevas = new List<CuadrillaActividad>();
            var cruces = new List<string>();
            var omitidas = 0;

            foreach (var o in fuente.OrderBy(f => f.Fecha).ThenBy(f => f.HoraInicio))
            {
                if (!activosSet.Contains(o.TecnicoId)) { omitidas++; continue; }

                var fecha = destino.AddDays((o.Fecha.Date - origen).Days);
                var clave = $"{o.TecnicoId}|{fecha:yyyyMMdd}|{o.HoraInicio}|{o.HoraFin}|{o.Actividad}";
                if (!claves.Add(clave)) { omitidas++; continue; }

                var noLaboral = CuadrillaReglas.EsNoLaboral(o.TipoRegistro);
                var copia = new CuadrillaActividad
                {
                    Id = Guid.NewGuid(),
                    Fecha = fecha,
                    TecnicoId = o.TecnicoId,
                    CuadrillaId = o.CuadrillaId,
                    ProyectoId = o.ProyectoId,
                    ProyectoReferencia = o.ProyectoReferencia,
                    LineaNegocio = o.LineaNegocio,
                    TipoRegistro = o.TipoRegistro,
                    Actividad = o.Actividad,
                    OT = o.OT,
                    HoraInicio = o.HoraInicio,
                    HoraFin = o.HoraFin,
                    HorasPlaneadas = o.HorasPlaneadas,
                    Prioridad = o.Prioridad,
                    Estado = noLaboral ? o.TipoRegistro : "Planeada",
                    EstadoPlanificacion = "Programada",
                    SupervisorTipo = o.SupervisorTipo,
                    SupervisorContactoId = o.SupervisorContactoId,
                    SupervisorUsuarioId = o.SupervisorUsuarioId,
                    SupervisorNombre = o.SupervisorNombre,
                    CreadoPorId = usuarioId,
                    FechaCreacion = DateTime.UtcNow
                };

                try { await VerificarCruceAsync(copia, permitirCruce == true, context); }
                catch (GraphQLException ex) when (ex.Errors.Any(e => e.Code == "CRUCE_HORARIO"))
                {
                    nombres.TryGetValue(o.TecnicoId, out var n);
                    cruces.Add($"{n ?? "Técnico"} {fecha:yyyy-MM-dd}");
                    continue;
                }
                nuevas.Add(copia);
            }

            if (cruces.Count > 0)
                throw CruceLote(cruces);

            context.CuadrillasActividades.AddRange(nuevas);
            context.CuadrillasActividadHistorial.AddRange(nuevas.Select(a =>
                NuevoHistorial(a.Id, usuarioId, $"Copiada desde la semana del {origen:yyyy-MM-dd}")));
            await context.SaveChangesAsync();

            return new CuadrillaCopiaResultadoDto
            {
                Copiadas = nuevas.Count,
                Omitidas = omitidas,
                Detalle = omitidas > 0 ? "Se omitieron registros ya existentes en la semana destino o de técnicos inactivos." : null
            };
        }

        // ---------------------------------------------------------
        // ACTUALIZAR / REPROGRAMAR ACTIVIDAD (programar)
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<CuadrillaActividad> ActualizarCuadrillaActividad(
            Guid id,
            CuadrillaActividadInput input,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, _, puedeConfig) = CuadrillaAcceso.Contexto(http);
            ValidarUsuarioActual(usuarioId);

            var actividad = await context.CuadrillasActividades.FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new GraphQLException("La actividad no existe.");

            // Aplica a la fecha original y a la nueva
            await CuadrillaReglas.ValidarFechaRetroactivaAsync(context, actividad.Fecha, puedeConfig);
            await CuadrillaReglas.ValidarFechaRetroactivaAsync(context, input.Fecha, puedeConfig);

            var antes = new
            {
                actividad.Estado, actividad.Fecha, actividad.TecnicoId, actividad.HoraInicio,
                actividad.HoraFin, actividad.ProyectoId, actividad.ProyectoReferencia, actividad.TipoRegistro
            };

            await AplicarPlanificacionAsync(actividad, input, context);
            await VerificarCruceAsync(actividad, input.PermitirCruce == true, context);

            if (actividad.Estado == "Finalizada" && actividad.FechaCierre == null)
                actividad.FechaCierre = CuadrillaReglas.HoyColombia();

            actividad.ActualizadoPorId = usuarioId;
            actividad.FechaActualizacion = DateTime.UtcNow;

            var cambios = new List<string>();
            if (antes.Estado != actividad.Estado) cambios.Add($"Estado {antes.Estado} → {actividad.Estado}");
            if (antes.Fecha.Date != actividad.Fecha.Date) cambios.Add($"Fecha {antes.Fecha:yyyy-MM-dd} → {actividad.Fecha:yyyy-MM-dd}");
            if (antes.TecnicoId != actividad.TecnicoId) cambios.Add("Técnico reasignado");
            if (antes.HoraInicio != actividad.HoraInicio || antes.HoraFin != actividad.HoraFin)
                cambios.Add($"Horario {antes.HoraInicio ?? "—"}–{antes.HoraFin ?? "—"} → {actividad.HoraInicio ?? "—"}–{actividad.HoraFin ?? "—"}");
            if (antes.ProyectoId != actividad.ProyectoId || antes.ProyectoReferencia != actividad.ProyectoReferencia)
                cambios.Add("Proyecto / sede modificado");
            if (antes.TipoRegistro != actividad.TipoRegistro) cambios.Add($"Tipo {antes.TipoRegistro} → {actividad.TipoRegistro}");

            context.CuadrillasActividadHistorial.Add(NuevoHistorial(
                actividad.Id, usuarioId, cambios.Count > 0 ? string.Join("; ", cambios) : "Registro actualizado"));

            await context.SaveChangesAsync();
            return await CargarAsync(context, actividad.Id);
        }

        // ---------------------------------------------------------
        // ACTUALIZAR EJECUCIÓN (técnico dueño o quien programa)
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.ver")]
        public async Task<CuadrillaActividad> ActualizarEjecucionCuadrillaActividad(
            Guid id,
            CuadrillaEjecucionInput input,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, verTodas, _) = CuadrillaAcceso.Contexto(http);
            ValidarUsuarioActual(usuarioId);

            var actividad = await context.CuadrillasActividades.FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new GraphQLException("La actividad no existe.");

            if (!verTodas && actividad.TecnicoId != usuarioId)
                throw new GraphQLException("No tienes acceso a esta actividad.");

            if (CuadrillaReglas.EsNoLaboral(actividad.TipoRegistro))
                throw new GraphQLException("Los permisos, descansos y registros de disponibilidad no tienen ejecución.");

            var estado = (input.Estado ?? string.Empty).Trim();
            if (!CuadrillaReglas.EstadosEjecucion.Contains(estado))
                throw new GraphQLException("El estado de ejecución no es válido.");
            if (!verTodas && !EstadosQuePuedeUsarTecnico.Contains(estado))
                throw new GraphQLException("Solo quien programa puede marcar una actividad como Reprogramada.");

            if (input.HorasReales.HasValue && (input.HorasReales.Value < 0 || input.HorasReales.Value > 24))
                throw new GraphQLException("Las horas reales deben estar entre 0 y 24.");

            var detalle = Limpiar(input.DetalleBloqueo, 1000);
            if (estado == "Bloqueada" && detalle == null)
                throw new GraphQLException("Describe el bloqueo o impedimento de la actividad.");

            var estadoAnterior = actividad.Estado;
            var horasAnteriores = actividad.HorasReales;

            actividad.Estado = estado;
            actividad.HorasReales = input.HorasReales;
            actividad.TipoNovedad = Limpiar(input.TipoNovedad, 50);
            actividad.DetalleBloqueo = detalle;
            actividad.SiguienteAccion = Limpiar(input.SiguienteAccion, 1000);
            if (input.Observaciones != null) actividad.Observaciones = Limpiar(input.Observaciones, 1000);

            if (estado == "Finalizada")
                actividad.FechaCierre = input.FechaCierre?.Date ?? actividad.FechaCierre ?? CuadrillaReglas.HoyColombia();
            else
                actividad.FechaCierre = input.FechaCierre?.Date;

            actividad.ActualizadoPorId = usuarioId;
            actividad.FechaActualizacion = DateTime.UtcNow;

            var cambios = new List<string>();
            if (estadoAnterior != estado) cambios.Add($"Estado {estadoAnterior} → {estado}");
            if (horasAnteriores != input.HorasReales)
                cambios.Add($"Horas reales {(horasAnteriores?.ToString("0.##") ?? "—")} → {(input.HorasReales?.ToString("0.##") ?? "—")}");
            if (estado == "Bloqueada") cambios.Add("Bloqueo: " + detalle);

            context.CuadrillasActividadHistorial.Add(NuevoHistorial(
                actividad.Id, usuarioId, cambios.Count > 0 ? string.Join("; ", cambios) : "Ejecución actualizada"));

            await context.SaveChangesAsync();
            return await CargarAsync(context, actividad.Id);
        }

        // ---------------------------------------------------------
        // ELIMINAR ACTIVIDAD (programar)
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<bool> EliminarCuadrillaActividad(
            Guid id,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (_, _, puedeConfig) = CuadrillaAcceso.Contexto(http);
            var actividad = await context.CuadrillasActividades.FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new GraphQLException("La actividad no existe.");
            await CuadrillaReglas.ValidarFechaRetroactivaAsync(context, actividad.Fecha, puedeConfig);

            var historial = await context.CuadrillasActividadHistorial.Where(h => h.ActividadId == id).ToListAsync();
            context.CuadrillasActividadHistorial.RemoveRange(historial);
            context.CuadrillasActividades.Remove(actividad);
            await context.SaveChangesAsync();
            return true;
        }

        // ---------------------------------------------------------
        // DÍAS IMPRODUCTIVOS MANUALES (programar)
        //   MARCADO    = agrega un día que el sistema no detectó
        //   JUSTIFICADO = excluye un día que el sistema detectó (imprevisto, fuerza mayor…)
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<CuadrillaDiaImproductivo> MarcarCuadrillaDiaImproductivo(
            Guid tecnicoId,
            DateTime fecha,
            string tipo,
            string motivo,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, _, _) = CuadrillaAcceso.Contexto(http);
            ValidarUsuarioActual(usuarioId);

            tipo = (tipo ?? string.Empty).Trim().ToUpperInvariant();
            if (tipo != "MARCADO" && tipo != "JUSTIFICADO")
                throw new GraphQLException("El tipo debe ser MARCADO o JUSTIFICADO.");

            var motivoLimpio = Limpiar(motivo, 300);
            if (motivoLimpio == null) throw new GraphQLException("El motivo es obligatorio.");

            if (!await context.Usuarios.AnyAsync(u => u.Id == tecnicoId))
                throw new GraphQLException("El técnico no existe.");

            var dia = fecha.Date;
            var existente = await context.CuadrillasDiasImproductivos
                .FirstOrDefaultAsync(d => d.TecnicoId == tecnicoId && d.Fecha == dia);

            if (existente != null)
            {
                existente.Tipo = tipo;
                existente.Motivo = motivoLimpio;
                existente.CreadoPorId = usuarioId;
                existente.FechaCreacion = DateTime.UtcNow;
            }
            else
            {
                existente = new CuadrillaDiaImproductivo
                {
                    TecnicoId = tecnicoId,
                    Fecha = dia,
                    Tipo = tipo,
                    Motivo = motivoLimpio,
                    CreadoPorId = usuarioId
                };
                context.CuadrillasDiasImproductivos.Add(existente);
            }

            await context.SaveChangesAsync();
            return existente;
        }

        [Authorize(Policy = "proyectos.cuadrillas.programar")]
        public async Task<bool> EliminarCuadrillaDiaImproductivo(Guid id, [Service] ApplicationDbContext context)
        {
            var dia = await context.CuadrillasDiasImproductivos.FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new GraphQLException("El registro no existe.");
            context.CuadrillasDiasImproductivos.Remove(dia);
            await context.SaveChangesAsync();
            return true;
        }

        // ---------------------------------------------------------
        // COMPENSATORIOS (config): total acumulado por técnico
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.config")]
        public async Task<bool> GuardarCuadrillaCompensatorio(
            Guid tecnicoId,
            decimal diasAcumulados,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, _, _) = CuadrillaAcceso.Contexto(http);

            if (diasAcumulados < 0 || diasAcumulados > 999)
                throw new GraphQLException("Los días acumulados deben estar entre 0 y 999.");
            if (!await context.Usuarios.AnyAsync(u => u.Id == tecnicoId))
                throw new GraphQLException("El técnico no existe.");

            var fila = await context.CuadrillasCompensatorios.FirstOrDefaultAsync(c => c.TecnicoId == tecnicoId);
            if (fila == null)
            {
                fila = new CuadrillaCompensatorio { TecnicoId = tecnicoId };
                context.CuadrillasCompensatorios.Add(fila);
            }
            fila.DiasAcumulados = Math.Round(diasAcumulados, 1);
            fila.ActualizadoPorId = usuarioId == Guid.Empty ? (Guid?)null : usuarioId;
            fila.FechaActualizacion = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }

        // ---------------------------------------------------------
        // PARÁMETROS GENERALES (config)
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.config")]
        public async Task<CuadrillaParametrosDto> GuardarCuadrillaParametros(
            int diasMaxRetroactivo,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, _, _) = CuadrillaAcceso.Contexto(http);
            ValidarUsuarioActual(usuarioId);

            if (diasMaxRetroactivo < 0 || diasMaxRetroactivo > 365)
                throw new GraphQLException("Los días hacia atrás deben estar entre 0 y 365.");

            var fila = await context.CuadrillasParametros
                .FirstOrDefaultAsync(p => p.Clave == CuadrillaReglas.ParamDiasRetroactivo);
            if (fila == null)
            {
                fila = new CuadrillaParametro { Clave = CuadrillaReglas.ParamDiasRetroactivo };
                context.CuadrillasParametros.Add(fila);
            }
            fila.Valor = diasMaxRetroactivo.ToString();
            fila.ActualizadoEn = DateTime.UtcNow;
            fila.ActualizadoPorId = usuarioId;

            await context.SaveChangesAsync();
            return new CuadrillaParametrosDto(diasMaxRetroactivo);
        }

        // ---------------------------------------------------------
        // BASE DE HORAS (config)
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.config")]
        public async Task<CuadrillaConfigHoras> GuardarCuadrillaConfigHoras(
            CuadrillaConfigHorasInput input,
            [Service] ApplicationDbContext context,
            [Service] IHttpContextAccessor http)
        {
            var (usuarioId, _, _) = CuadrillaAcceso.Contexto(http);
            ValidarUsuarioActual(usuarioId);

            if (input.HorasSemanales < 0 || input.HorasSemanales > 48)
                throw new GraphQLException("Las horas semanales deben estar entre 0 y 48.");
            if (input.DiasLaborales != 5 && input.DiasLaborales != 6)
                throw new GraphQLException("Selecciona una distribución de 5 o 6 días laborales.");
            if (input.TecnicoId.HasValue && !await context.Usuarios.AnyAsync(u => u.Id == input.TecnicoId.Value))
                throw new GraphQLException("El técnico no existe.");

            var vigente = input.VigenteDesde.Date;
            var fila = await context.CuadrillasConfigHoras
                .FirstOrDefaultAsync(c => c.TecnicoId == input.TecnicoId && c.VigenteDesde == vigente);

            if (fila == null)
            {
                fila = new CuadrillaConfigHoras
                {
                    TecnicoId = input.TecnicoId,
                    VigenteDesde = vigente,
                    CreadoPorId = usuarioId
                };
                context.CuadrillasConfigHoras.Add(fila);
            }

            fila.HorasSemanales = Math.Round(input.HorasSemanales, 1);
            fila.DiasLaborales = (byte)input.DiasLaborales;

            await context.SaveChangesAsync();
            return fila;
        }

        [Authorize(Policy = "proyectos.cuadrillas.config")]
        public async Task<bool> EliminarCuadrillaConfigHoras(Guid id, [Service] ApplicationDbContext context)
        {
            var fila = await context.CuadrillasConfigHoras.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new GraphQLException("La configuración no existe.");

            if (fila.TecnicoId == null)
            {
                var generales = await context.CuadrillasConfigHoras.CountAsync(c => c.TecnicoId == null);
                if (generales <= 1)
                    throw new GraphQLException("Debe existir al menos una configuración general de horas.");
            }

            context.CuadrillasConfigHoras.Remove(fila);
            await context.SaveChangesAsync();
            return true;
        }

        // ---------------------------------------------------------
        // FESTIVOS (config)
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.config")]
        public async Task<CuadrillaFestivo> GuardarCuadrillaFestivo(
            DateTime fecha,
            string nombre,
            [Service] ApplicationDbContext context)
        {
            var nombreLimpio = Limpiar(nombre, 100) ?? throw new GraphQLException("El nombre del festivo es obligatorio.");
            var dia = fecha.Date;

            var fila = await context.CuadrillasFestivos.FirstOrDefaultAsync(f => f.Fecha == dia);
            if (fila == null)
            {
                fila = new CuadrillaFestivo { Fecha = dia };
                context.CuadrillasFestivos.Add(fila);
            }
            fila.Nombre = nombreLimpio;

            await context.SaveChangesAsync();
            return fila;
        }

        [Authorize(Policy = "proyectos.cuadrillas.config")]
        public async Task<bool> EliminarCuadrillaFestivo(DateTime fecha, [Service] ApplicationDbContext context)
        {
            var dia = fecha.Date;
            var fila = await context.CuadrillasFestivos.FirstOrDefaultAsync(f => f.Fecha == dia)
                ?? throw new GraphQLException("El festivo no existe.");
            context.CuadrillasFestivos.Remove(fila);
            await context.SaveChangesAsync();
            return true;
        }

        // ---------------------------------------------------------
        // CUADRILLAS Y MIEMBROS (config)
        // ---------------------------------------------------------
        [Authorize(Policy = "proyectos.cuadrillas.config")]
        public async Task<Cuadrilla> GuardarCuadrilla(
            Guid? id,
            string nombre,
            string? lineaNegocio,
            bool activa,
            [Service] ApplicationDbContext context)
        {
            var nombreLimpio = Limpiar(nombre, 100) ?? throw new GraphQLException("El nombre de la cuadrilla es obligatorio.");
            var linea = Limpiar(lineaNegocio, 50);
            if (linea != null && linea != "civiles" && linea != "energia" && linea != "telecomunicaciones")
                throw new GraphQLException("La línea de negocio debe ser civiles, energia o telecomunicaciones.");

            if (await context.Cuadrillas.AnyAsync(c => c.Nombre == nombreLimpio && c.Id != (id ?? Guid.Empty)))
                throw new GraphQLException("Ya existe una cuadrilla con ese nombre.");

            Cuadrilla cuadrilla;
            if (id.HasValue)
            {
                cuadrilla = await context.Cuadrillas.FirstOrDefaultAsync(c => c.Id == id.Value)
                    ?? throw new GraphQLException("La cuadrilla no existe.");
            }
            else
            {
                cuadrilla = new Cuadrilla();
                context.Cuadrillas.Add(cuadrilla);
            }

            cuadrilla.Nombre = nombreLimpio;
            cuadrilla.LineaNegocio = linea;
            cuadrilla.Activa = activa;

            await context.SaveChangesAsync();
            return cuadrilla;
        }

        [Authorize(Policy = "proyectos.cuadrillas.config")]
        public async Task<bool> AgregarMiembroCuadrilla(
            Guid cuadrillaId,
            Guid usuarioId,
            [Service] ApplicationDbContext context)
        {
            if (!await context.Cuadrillas.AnyAsync(c => c.Id == cuadrillaId))
                throw new GraphQLException("La cuadrilla no existe.");
            if (!await context.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Estado == "ACTIVO"))
                throw new GraphQLException("El usuario no existe o no está activo.");

            var yaEsMiembro = await context.CuadrillasMiembros
                .AnyAsync(m => m.CuadrillaId == cuadrillaId && m.UsuarioId == usuarioId);
            if (!yaEsMiembro)
            {
                context.CuadrillasMiembros.Add(new CuadrillaMiembro { CuadrillaId = cuadrillaId, UsuarioId = usuarioId });
                await context.SaveChangesAsync();
            }
            return true;
        }

        [Authorize(Policy = "proyectos.cuadrillas.config")]
        public async Task<bool> QuitarMiembroCuadrilla(
            Guid cuadrillaId,
            Guid usuarioId,
            [Service] ApplicationDbContext context)
        {
            var miembro = await context.CuadrillasMiembros
                .FirstOrDefaultAsync(m => m.CuadrillaId == cuadrillaId && m.UsuarioId == usuarioId);
            if (miembro != null)
            {
                context.CuadrillasMiembros.Remove(miembro);
                await context.SaveChangesAsync();
            }
            return true;
        }

        // =========================================================
        // HELPERS
        // =========================================================
        private static DateTime LunesDe(DateTime fecha)
        {
            var d = fecha.Date;
            var delta = ((int)d.DayOfWeek + 6) % 7; // lunes = 0
            return d.AddDays(-delta);
        }

        private static GraphQLException CruceLote(List<string> cruces)
        {
            var muestra = string.Join(", ", cruces.Take(6)) + (cruces.Count > 6 ? $" y {cruces.Count - 6} más" : "");
            return new GraphQLException(ErrorBuilder.New()
                .SetMessage($"Hay cruces de horario en {cruces.Count} registro(s): {muestra}. Confirma si deseas guardar de todas formas.")
                .SetCode("CRUCE_HORARIO")
                .Build());
        }

        private static void ValidarUsuarioActual(Guid usuarioId)
        {
            if (usuarioId == Guid.Empty)
                throw new GraphQLException("No se pudo identificar al usuario de la sesión.");
        }

        private static string? Limpiar(string? valor, int max)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var v = valor.Trim();
            return v.Length > max ? v.Substring(0, max) : v;
        }

        private static CuadrillaActividadHistorial NuevoHistorial(Guid actividadId, Guid usuarioId, string texto) =>
            new CuadrillaActividadHistorial
            {
                ActividadId = actividadId,
                UsuarioId = usuarioId == Guid.Empty ? (Guid?)null : usuarioId,
                Texto = texto.Length > 500 ? texto.Substring(0, 500) : texto
            };

        private static async Task<CuadrillaActividad> CargarAsync(ApplicationDbContext context, Guid id)
        {
            return await context.CuadrillasActividades
                .AsNoTracking()
                .Include(a => a.Tecnico)
                .Include(a => a.Cuadrilla)
                .Include(a => a.Proyecto)
                .Include(a => a.SupervisorContacto)
                .Include(a => a.SupervisorUsuario)
                .Include(a => a.Historial).ThenInclude(h => h.Usuario)
                .FirstAsync(a => a.Id == id);
        }

        /// <summary>Valida el input de planificación y lo aplica a la entidad (nueva o existente).</summary>
        private static async Task AplicarPlanificacionAsync(
            CuadrillaActividad a, CuadrillaActividadInput i, ApplicationDbContext context)
        {
            // --- Tipo de registro ---
            var tipo = (i.TipoRegistro ?? string.Empty).Trim();
            if (!CuadrillaReglas.TiposRegistro.Contains(tipo))
                throw new GraphQLException("El tipo de registro no es válido.");
            var noLaboral = CuadrillaReglas.EsNoLaboral(tipo);

            // --- Técnico ---
            var tecnicoOk = await context.Usuarios.AnyAsync(u => u.Id == i.TecnicoId && u.Estado == "ACTIVO");
            if (!tecnicoOk) throw new GraphQLException("El técnico no existe o no está activo.");

            // --- Actividad / motivo ---
            var actividad = Limpiar(i.Actividad, 300);
            if (actividad == null)
            {
                actividad = tipo switch
                {
                    "Permiso" => "PERMISO DEL TÉCNICO",
                    "Descanso" => "DESCANSO / COMPENSATORIO",
                    "Disponible" => "DISPONIBLE - JORNADA SIN ASIGNACIÓN",
                    _ => null
                };
            }
            if (actividad == null) throw new GraphQLException("La actividad / motivo es obligatoria.");

            // --- Horario ---
            var inicio = Limpiar(i.HoraInicio, 5);
            var fin = Limpiar(i.HoraFin, 5);
            if (inicio != null || fin != null)
            {
                var mi = CuadrillaReglas.Minutos(inicio);
                var mf = CuadrillaReglas.Minutos(fin);
                if (mi == null || mf == null)
                    throw new GraphQLException("Indica hora de inicio y hora de fin con formato HH:mm.");
                if (mf <= mi)
                    throw new GraphQLException("La hora final debe ser posterior a la inicial.");
            }

            // --- Prioridad / planificación ---
            var prioridad = Limpiar(i.Prioridad, 20) ?? "Media";
            if (!CuadrillaReglas.Prioridades.Contains(prioridad))
                throw new GraphQLException("La prioridad no es válida.");

            var planificacion = Limpiar(i.EstadoPlanificacion, 30) ?? "Programada";
            if (!CuadrillaReglas.EstadosPlanificacion.Contains(planificacion))
                throw new GraphQLException("El estado de planificación no es válido.");

            // --- Estado de ejecución ---
            string estado;
            if (noLaboral)
            {
                estado = tipo; // Permiso | Descanso | Disponible
            }
            else
            {
                estado = Limpiar(i.Estado, 30) ?? (CuadrillaReglas.EstadosEjecucion.Contains(a.Estado) ? a.Estado : "Planeada");
                if (!CuadrillaReglas.EstadosEjecucion.Contains(estado))
                    throw new GraphQLException("El estado de ejecución no es válido.");
            }

            // --- Horas planeadas ---
            decimal? planeadas = null;
            if (!noLaboral && i.HorasPlaneadas.HasValue)
            {
                if (i.HorasPlaneadas.Value < 0 || i.HorasPlaneadas.Value > 24)
                    throw new GraphQLException("Las horas planeadas deben estar entre 0 y 24.");
                planeadas = Math.Round(i.HorasPlaneadas.Value, 2);
            }

            // --- Proyecto / sede / línea de negocio ---
            string? linea = Limpiar(i.LineaNegocio, 50);
            var referencia = Limpiar(i.ProyectoReferencia, 150);
            Guid? clienteIdProyecto = null;

            if (i.ProyectoId.HasValue)
            {
                var proyecto = await context.Proyectos.AsNoTracking()
                    .Where(p => p.Id == i.ProyectoId.Value)
                    .Select(p => new { p.Id, p.LineaNegocio, p.ClienteId })
                    .FirstOrDefaultAsync()
                    ?? throw new GraphQLException("El proyecto seleccionado no existe.");
                clienteIdProyecto = proyecto.ClienteId;
                linea ??= string.IsNullOrWhiteSpace(proyecto.LineaNegocio) ? null : proyecto.LineaNegocio;
                referencia = null; // con proyecto enlazado no se usa el texto libre
            }
            else if (!noLaboral && referencia == null)
            {
                throw new GraphQLException("Selecciona un proyecto o escribe la sede.");
            }

            // --- Supervisor (cliente o interno) ---
            string? supTipo = Limpiar(i.SupervisorTipo, 10)?.ToUpperInvariant();
            Guid? supContacto = null, supUsuario = null;
            string? supNombre = Limpiar(i.SupervisorNombre, 150);

            if (supTipo == null)
            {
                supNombre = null;
            }
            else if (supTipo == "CLIENTE")
            {
                if (i.SupervisorContactoId.HasValue)
                {
                    var contacto = await context.ClientesContactos.AsNoTracking()
                        .Where(c => c.Id == i.SupervisorContactoId.Value)
                        .Select(c => new { c.Id, c.Nombre, ClienteId = c.Sucursal != null ? c.Sucursal.ClienteId : Guid.Empty })
                        .FirstOrDefaultAsync()
                        ?? throw new GraphQLException("El contacto del cliente no existe.");
                    if (clienteIdProyecto.HasValue && contacto.ClienteId != clienteIdProyecto.Value)
                        throw new GraphQLException("El contacto seleccionado no pertenece al cliente del proyecto.");
                    supContacto = contacto.Id;
                    supNombre = contacto.Nombre;
                }
                else if (supNombre == null)
                {
                    throw new GraphQLException("Selecciona el supervisor del cliente o escribe su nombre.");
                }
            }
            else if (supTipo == "INTERNO")
            {
                if (i.SupervisorUsuarioId.HasValue)
                {
                    var usuario = await context.Usuarios.AsNoTracking()
                        .Where(u => u.Id == i.SupervisorUsuarioId.Value)
                        .Select(u => new { u.Id, u.Nombre })
                        .FirstOrDefaultAsync()
                        ?? throw new GraphQLException("El supervisor interno no existe.");
                    supUsuario = usuario.Id;
                    supNombre = usuario.Nombre;
                }
                else if (supNombre == null)
                {
                    throw new GraphQLException("Selecciona el supervisor interno o escribe su nombre.");
                }
            }
            else
            {
                throw new GraphQLException("El tipo de supervisor debe ser CLIENTE o INTERNO.");
            }

            // --- Cuadrilla: si no viene, la del técnico ---
            var cuadrillaId = i.CuadrillaId;
            if (!cuadrillaId.HasValue)
            {
                cuadrillaId = await context.CuadrillasMiembros
                    .Where(m => m.UsuarioId == i.TecnicoId && m.Cuadrilla != null && m.Cuadrilla.Activa)
                    .Select(m => (Guid?)m.CuadrillaId)
                    .FirstOrDefaultAsync();
            }

            // --- Asignación ---
            a.Fecha = i.Fecha.Date;
            a.TecnicoId = i.TecnicoId;
            a.CuadrillaId = cuadrillaId;
            a.ProyectoId = i.ProyectoId;
            a.ProyectoReferencia = referencia;
            a.LineaNegocio = linea;
            a.TipoRegistro = tipo;
            a.Actividad = actividad;
            a.OT = Limpiar(i.OT, 50);
            a.HoraInicio = inicio;
            a.HoraFin = fin;
            a.HorasPlaneadas = planeadas;
            a.Prioridad = prioridad;
            a.Estado = estado;
            a.EstadoPlanificacion = planificacion;
            a.SupervisorTipo = supTipo;
            a.SupervisorContactoId = supContacto;
            a.SupervisorUsuarioId = supUsuario;
            a.SupervisorNombre = supNombre;
            if (i.Observaciones != null) a.Observaciones = Limpiar(i.Observaciones, 1000);

            if (noLaboral)
            {
                a.HorasReales = null;
                a.TipoNovedad = null;
                a.DetalleBloqueo = null;
                a.SiguienteAccion = null;
            }
        }

        /// <summary>Si el técnico ya tiene otra actividad que se cruza en horario ese día, avisa (código CRUCE_HORARIO).</summary>
        private static async Task VerificarCruceAsync(CuadrillaActividad a, bool permitirCruce, ApplicationDbContext context)
        {
            if (permitirCruce || a.HoraInicio == null || a.HoraFin == null) return;

            var ini = CuadrillaReglas.Minutos(a.HoraInicio);
            var fin = CuadrillaReglas.Minutos(a.HoraFin);
            if (ini == null || fin == null) return;

            var fecha = a.Fecha.Date;
            var delDia = await context.CuadrillasActividades.AsNoTracking()
                .Where(x => x.TecnicoId == a.TecnicoId && x.Fecha == fecha && x.Id != a.Id
                            && x.EstadoPlanificacion != "Cancelada" && x.HoraInicio != null && x.HoraFin != null)
                .ToListAsync();

            var choques = delDia.Where(x =>
            {
                var xi = CuadrillaReglas.Minutos(x.HoraInicio);
                var xf = CuadrillaReglas.Minutos(x.HoraFin);
                return xi != null && xf != null && xi < fin && xf > ini;
            }).ToList();

            if (choques.Count == 0) return;

            var detalle = string.Join("; ", choques.Select(x => $"{x.HoraInicio}–{x.HoraFin} · {x.Actividad}"));
            throw new GraphQLException(ErrorBuilder.New()
                .SetMessage($"Cruce horario con: {detalle}. Confirma si deseas guardar de todas formas.")
                .SetCode("CRUCE_HORARIO")
                .Build());
        }
    }
}
