using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Cuadrillas;

namespace RmsErp.Api.Services
{
    // =========================================================
    // DTOs de resultado
    // =========================================================
    public class CuadrillaIndicadorDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public double? Valor { get; set; }
        public double Numerador { get; set; }
        public double Denominador { get; set; }
        public double? Meta { get; set; }
        public double? Peso { get; set; }
        public bool TieneDatos { get; set; }
        public bool? Cumple { get; set; }
        public string Formula { get; set; } = string.Empty;
        public string Frecuencia { get; set; } = "Mensual";
    }

    public class CuadrillaDiaImproductivoDto
    {
        public Guid TecnicoId { get; set; }
        public string TecnicoNombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        // AUTOMATICO | MANUAL | JUSTIFICADO
        public string Origen { get; set; } = "AUTOMATICO";
        public string Motivo { get; set; } = string.Empty;
        public bool Justificado { get; set; }
    }

    public class CuadrillaKpiBloque
    {
        public List<CuadrillaIndicadorDto> Indicadores { get; set; } = new List<CuadrillaIndicadorDto>();
        public double HorasProductivas { get; set; }
        public double HorasDisponibles { get; set; }
        public int DiasImproductivos { get; set; }
        public int DiasJustificados { get; set; }
    }

    public class CuadrillaKpiTecnicoDto
    {
        public Guid TecnicoId { get; set; }
        public string TecnicoNombre { get; set; } = string.Empty;
        public CuadrillaKpiBloque Resultado { get; set; } = new CuadrillaKpiBloque();
    }

    /// <summary>
    /// Alerta de programación.
    /// Tipo: VENCIDA | BLOQUEADA | CRUCE | SIN_ASIGNACION | DISPONIBLE | TIEMPO_LIBRE
    /// </summary>
    public class CuadrillaAlertaDto
    {
        public string Tipo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public Guid TecnicoId { get; set; }
        public string TecnicoNombre { get; set; } = string.Empty;
        public Guid? ActividadId { get; set; }
        public string? Proyecto { get; set; }
        public string? Actividad { get; set; }
        public double? HorasProgramadas { get; set; }
        public double? Jornada { get; set; }
        public double? HorasLibres { get; set; }
        public string Detalle { get; set; } = string.Empty;
    }

    public class CuadrillaAlertasResult
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public double UmbralHorasLibres { get; set; }
        public List<CuadrillaAlertaDto> Alertas { get; set; } = new List<CuadrillaAlertaDto>();
    }

    public class CuadrillaKpiResult
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        /// <summary>Último día efectivamente medido (los días futuros no cuentan).</summary>
        public DateTime CorteHasta { get; set; }
        public CuadrillaKpiBloque Global { get; set; } = new CuadrillaKpiBloque();
        public List<CuadrillaKpiTecnicoDto> PorTecnico { get; set; } = new List<CuadrillaKpiTecnicoDto>();
        public List<CuadrillaDiaImproductivoDto> DiasImproductivos { get; set; } = new List<CuadrillaDiaImproductivoDto>();
        public List<string> Notas { get; set; } = new List<string>();
    }

    // =========================================================
    // Servicio de cálculo (única fuente de verdad de los KPI)
    // =========================================================
    public class CuadrillaKpiService
    {
        private readonly ApplicationDbContext _db;

        public CuadrillaKpiService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<CuadrillaKpiResult> CalcularAsync(
            DateTime desde, DateTime hasta, Guid? tecnicoId, Guid? cuadrillaId, Guid? proyectoId)
        {
            desde = desde.Date;
            hasta = hasta.Date;
            if (hasta < desde) throw new GraphQLException("El rango de fechas no es válido.");
            if ((hasta - desde).TotalDays > 400) throw new GraphQLException("El rango máximo para los indicadores es de 400 días.");

            var hoy = CuadrillaReglas.HoyColombia();
            var corte = hasta > hoy ? hoy : hasta;

            // --- 1. Técnicos en alcance ---
            List<Guid> tecnicoIds;
            if (tecnicoId.HasValue)
            {
                tecnicoIds = new List<Guid> { tecnicoId.Value };
            }
            else if (cuadrillaId.HasValue)
            {
                tecnicoIds = await _db.CuadrillasMiembros
                    .Where(m => m.CuadrillaId == cuadrillaId.Value)
                    .Select(m => m.UsuarioId).Distinct().ToListAsync();
            }
            else
            {
                var miembros = await _db.CuadrillasMiembros
                    .Where(m => m.Cuadrilla != null && m.Cuadrilla.Activa)
                    .Select(m => m.UsuarioId).Distinct().ToListAsync();
                var conActividad = await _db.CuadrillasActividades
                    .Where(a => a.Fecha >= desde && a.Fecha <= hasta)
                    .Select(a => a.TecnicoId).Distinct().ToListAsync();
                tecnicoIds = miembros.Union(conActividad).ToList();
            }

            var nombres = await _db.Usuarios
                .Where(u => tecnicoIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Nombre);

            // --- 2. Datos del período ---
            var acts = await _db.CuadrillasActividades.AsNoTracking()
                .Where(a => a.Fecha >= desde && a.Fecha <= hasta && tecnicoIds.Contains(a.TecnicoId))
                .ToListAsync();

            var manuales = await _db.CuadrillasDiasImproductivos.AsNoTracking()
                .Where(d => d.Fecha >= desde && d.Fecha <= hasta && tecnicoIds.Contains(d.TecnicoId))
                .ToListAsync();

            var festivos = (await _db.CuadrillasFestivos.AsNoTracking()
                .Where(f => f.Fecha >= desde && f.Fecha <= hasta)
                .Select(f => f.Fecha).ToListAsync())
                .Select(f => f.Date).ToHashSet();

            var configs = await _db.CuadrillasConfigHoras.AsNoTracking().ToListAsync();

            // --- 3. Cálculo global y por técnico ---
            var dias = new List<CuadrillaDiaImproductivoDto>();
            var global = CalcularBloque(tecnicoIds, desde, corte, proyectoId, acts, manuales, festivos, configs, nombres, dias);

            var porTecnico = new List<CuadrillaKpiTecnicoDto>();
            foreach (var t in tecnicoIds.OrderBy(x => nombres.TryGetValue(x, out var n) ? n : string.Empty))
            {
                var bloque = CalcularBloque(new List<Guid> { t }, desde, corte, proyectoId, acts, manuales, festivos, configs, nombres, new List<CuadrillaDiaImproductivoDto>());
                porTecnico.Add(new CuadrillaKpiTecnicoDto
                {
                    TecnicoId = t,
                    TecnicoNombre = nombres.TryGetValue(t, out var nom) ? nom : "(técnico)",
                    Resultado = bloque
                });
            }

            var notas = new List<string>();
            if (corte < hasta)
                notas.Add($"Los indicadores se miden hasta el {corte:dd/MM/yyyy}; los días posteriores aún no cuentan.");
            if (proyectoId.HasValue)
                notas.Add("El filtro de proyecto aplica a cumplimiento de programación y visitas; utilización y días improductivos se calculan sobre toda la jornada del técnico.");

            return new CuadrillaKpiResult
            {
                Desde = desde,
                Hasta = hasta,
                CorteHasta = corte,
                Global = global,
                PorTecnico = porTecnico,
                DiasImproductivos = dias.OrderBy(d => d.Fecha).ThenBy(d => d.TecnicoNombre).ToList(),
                Notas = notas
            };
        }

        /// <summary>
        /// Alertas de la programación en el rango [desde, hasta] (por defecto la semana actual y la siguiente):
        ///  - SIN_ASIGNACION: día laboral sin ningún registro del técnico.
        ///  - DISPONIBLE: día laboral con registro "Disponible" y sin actividad operativa.
        ///  - TIEMPO_LIBRE: jornada (horas semanales / días laborales del técnico) menos horas programadas
        ///                  efectivas >= umbral configurable (parámetro HorasLibresAlerta).
        ///  - CRUCE: actividades del mismo técnico y día con horario traslapado.
        ///  - VENCIDA: fecha anterior a hoy y no finalizada (últimos 60 días).
        ///  - BLOQUEADA: actividad en estado Bloqueada (últimos 60 días hasta el fin del rango).
        /// Los días con permiso/descanso o justificados manualmente no generan alertas de asignación.
        /// </summary>
        public async Task<CuadrillaAlertasResult> AlertasAsync(DateTime desde, DateTime hasta)
        {
            desde = desde.Date;
            hasta = hasta.Date;
            if (hasta < desde) throw new GraphQLException("El rango de fechas no es válido.");
            if ((hasta - desde).TotalDays > 62) throw new GraphQLException("El rango máximo para las alertas es de 62 días.");

            var hoy = CuadrillaReglas.HoyColombia();
            var umbral = await CuadrillaReglas.HorasLibresAlertaAsync(_db);
            var inicioHistorial = hoy.AddDays(-60);
            var consultaDesde = desde < inicioHistorial ? desde : inicioHistorial;

            // Técnicos activos de cuadrillas activas + los que tienen registros en el rango
            var miembros = await _db.CuadrillasMiembros
                .Where(m => m.Cuadrilla != null && m.Cuadrilla.Activa)
                .Select(m => m.UsuarioId).Distinct().ToListAsync();
            var conRegistros = await _db.CuadrillasActividades
                .Where(a => a.Fecha >= desde && a.Fecha <= hasta)
                .Select(a => a.TecnicoId).Distinct().ToListAsync();
            var candidatos = miembros.Union(conRegistros).ToList();
            var nombres = await _db.Usuarios
                .Where(u => candidatos.Contains(u.Id) && u.Estado == "ACTIVO")
                .ToDictionaryAsync(u => u.Id, u => u.Nombre);
            var tecnicos = nombres.Keys.ToList();

            var acts = await _db.CuadrillasActividades.AsNoTracking()
                .Include(a => a.Proyecto)
                .Where(a => a.Fecha >= consultaDesde && a.Fecha <= hasta && tecnicos.Contains(a.TecnicoId))
                .ToListAsync();

            var manuales = await _db.CuadrillasDiasImproductivos.AsNoTracking()
                .Where(d => d.Fecha >= desde && d.Fecha <= hasta && tecnicos.Contains(d.TecnicoId) && d.Tipo == "JUSTIFICADO")
                .Select(d => new { d.TecnicoId, d.Fecha })
                .ToListAsync();
            var justificados = manuales.Select(m => $"{m.TecnicoId}|{m.Fecha:yyyyMMdd}").ToHashSet();

            var festivos = (await _db.CuadrillasFestivos.AsNoTracking()
                .Where(f => f.Fecha >= desde && f.Fecha <= hasta)
                .Select(f => f.Fecha).ToListAsync())
                .Select(f => f.Date).ToHashSet();

            var configs = await _db.CuadrillasConfigHoras.AsNoTracking().ToListAsync();

            string NombreProyecto(CuadrillaActividad a) =>
                a.Proyecto != null ? $"{a.Proyecto.Codigo} · {a.Proyecto.Nombre}" : (a.ProyectoReferencia ?? "");
            string Nombre(Guid t) => nombres.TryGetValue(t, out var n) ? n : "(técnico)";

            var alertas = new List<CuadrillaAlertaDto>();

            // --- Vencidas y bloqueadas ---
            foreach (var a in acts.Where(CuadrillaReglas.EsOperativa))
            {
                if (a.Fecha.Date < hoy && a.Estado != "Finalizada" && a.Estado != "Reprogramada")
                    alertas.Add(new CuadrillaAlertaDto
                    {
                        Tipo = "VENCIDA", Fecha = a.Fecha.Date, TecnicoId = a.TecnicoId, TecnicoNombre = Nombre(a.TecnicoId),
                        ActividadId = a.Id, Proyecto = NombreProyecto(a), Actividad = a.Actividad,
                        Detalle = $"Estado: {a.Estado}"
                    });
                if (a.Estado == "Bloqueada")
                    alertas.Add(new CuadrillaAlertaDto
                    {
                        Tipo = "BLOQUEADA", Fecha = a.Fecha.Date, TecnicoId = a.TecnicoId, TecnicoNombre = Nombre(a.TecnicoId),
                        ActividadId = a.Id, Proyecto = NombreProyecto(a), Actividad = a.Actividad,
                        Detalle = a.DetalleBloqueo ?? "Sin detalle del bloqueo"
                    });
            }

            // --- Cruces, sin asignación, disponibles y tiempo libre (por técnico y día del rango) ---
            var delRango = acts.Where(a => a.Fecha >= desde && a.Fecha <= hasta).ToList();
            foreach (var t in tecnicos)
            {
                var mias = delRango.Where(a => a.TecnicoId == t).ToList();
                for (var d = desde; d <= hasta; d = d.AddDays(1))
                {
                    var delDia = mias.Where(a => a.Fecha.Date == d && a.EstadoPlanificacion != "Cancelada").ToList();
                    var ops = delDia.Where(CuadrillaReglas.EsOperativa).ToList();

                    // Cruces de horario
                    var conHorario = ops.Where(a => CuadrillaReglas.Minutos(a.HoraInicio) != null && CuadrillaReglas.Minutos(a.HoraFin) != null).ToList();
                    for (var i = 0; i < conHorario.Count; i++)
                        for (var j = i + 1; j < conHorario.Count; j++)
                        {
                            var x = conHorario[i]; var y = conHorario[j];
                            if (CuadrillaReglas.Minutos(x.HoraInicio) < CuadrillaReglas.Minutos(y.HoraFin) &&
                                CuadrillaReglas.Minutos(y.HoraInicio) < CuadrillaReglas.Minutos(x.HoraFin))
                                alertas.Add(new CuadrillaAlertaDto
                                {
                                    Tipo = "CRUCE", Fecha = d, TecnicoId = t, TecnicoNombre = Nombre(t),
                                    ActividadId = x.Id, Proyecto = NombreProyecto(x), Actividad = x.Actividad,
                                    Detalle = $"{x.HoraInicio}–{x.HoraFin} se cruza con {y.HoraInicio}–{y.HoraFin} ({y.Actividad})"
                                });
                        }

                    // Jornada laboral del técnico
                    var (horasDia, diasLab) = ConfigPara(configs, t, d);
                    var laboral = d.DayOfWeek != DayOfWeek.Sunday
                                  && !festivos.Contains(d)
                                  && (diasLab == 6 || d.DayOfWeek != DayOfWeek.Saturday);
                    if (!laboral) continue;
                    if (delDia.Any(a => a.TipoRegistro == "Permiso" || a.TipoRegistro == "Descanso")) continue;
                    if (justificados.Contains($"{t}|{d:yyyyMMdd}")) continue;

                    var jornada = Math.Round(horasDia, 2);
                    if (ops.Count == 0)
                    {
                        var disponible = delDia.FirstOrDefault(a => a.TipoRegistro == "Disponible");
                        alertas.Add(new CuadrillaAlertaDto
                        {
                            Tipo = disponible != null ? "DISPONIBLE" : "SIN_ASIGNACION",
                            Fecha = d, TecnicoId = t, TecnicoNombre = Nombre(t),
                            ActividadId = disponible?.Id,
                            HorasProgramadas = 0, Jornada = jornada, HorasLibres = jornada,
                            Detalle = disponible != null ? "Disponible sin asignación" : "Sin actividades programadas en la jornada"
                        });
                        continue;
                    }

                    var programadas = Math.Round(ops.Sum(a => CuadrillaReglas.HorasPlaneadasEfectivas(a)), 2);
                    var libres = Math.Round(horasDia - programadas, 2);
                    if (libres >= umbral - 0.001)
                        alertas.Add(new CuadrillaAlertaDto
                        {
                            Tipo = "TIEMPO_LIBRE", Fecha = d, TecnicoId = t, TecnicoNombre = Nombre(t),
                            HorasProgramadas = programadas, Jornada = jornada, HorasLibres = libres,
                            Detalle = $"{libres:0.#} h libres de {jornada:0.#} h de jornada ({programadas:0.#} h programadas)"
                        });
                }
            }

            var orden = new[] { "VENCIDA", "BLOQUEADA", "CRUCE", "SIN_ASIGNACION", "DISPONIBLE", "TIEMPO_LIBRE" };
            return new CuadrillaAlertasResult
            {
                Desde = desde,
                Hasta = hasta,
                UmbralHorasLibres = umbral,
                Alertas = alertas
                    .OrderBy(a => Array.IndexOf(orden, a.Tipo))
                    .ThenBy(a => a.Fecha)
                    .ThenBy(a => a.TecnicoNombre)
                    .ToList()
            };
        }

        private static (double horasDia, int diasLab) ConfigPara(List<CuadrillaConfigHoras> configs, Guid tecnico, DateTime fecha)
        {
            var c = configs.Where(x => x.TecnicoId == tecnico && x.VigenteDesde.Date <= fecha)
                        .OrderByDescending(x => x.VigenteDesde).FirstOrDefault()
                 ?? configs.Where(x => x.TecnicoId == null && x.VigenteDesde.Date <= fecha)
                        .OrderByDescending(x => x.VigenteDesde).FirstOrDefault();

            var semanales = c != null ? (double)c.HorasSemanales : 42.0;
            var dias = c != null && c.DiasLaborales > 0 ? (int)c.DiasLaborales : 6;
            return (semanales / dias, dias);
        }

        private static CuadrillaKpiBloque CalcularBloque(
            List<Guid> techs, DateTime desde, DateTime corte, Guid? proyectoId,
            List<CuadrillaActividad> acts, List<CuadrillaDiaImproductivo> manuales,
            HashSet<DateTime> festivos, List<CuadrillaConfigHoras> configs,
            Dictionary<Guid, string> nombres, List<CuadrillaDiaImproductivoDto> diasOut)
        {
            double horasProd = 0, horasDisp = 0;
            int improductivos = 0, justificados = 0;

            foreach (var t in techs)
            {
                var nombre = nombres.TryGetValue(t, out var nm) ? nm : "(técnico)";
                var mias = acts.Where(a => a.TecnicoId == t).ToList();
                var ops = mias.Where(CuadrillaReglas.EsOperativa).ToList();
                var opsPorDia = ops.GroupBy(a => a.Fecha.Date).ToDictionary(g => g.Key, g => g.ToList());
                var ausencias = mias.Where(a => a.TipoRegistro == "Permiso" || a.TipoRegistro == "Descanso")
                                    .Select(a => a.Fecha.Date).ToHashSet();
                var disponibles = mias.Where(a => a.TipoRegistro == "Disponible")
                                      .Select(a => a.Fecha.Date).ToHashSet();
                var manualesTecnico = manuales.Where(m => m.TecnicoId == t)
                                              .GroupBy(m => m.Fecha.Date)
                                              .ToDictionary(g => g.Key, g => g.First());

                // Horas productivas: reales si existen; si no, las planeadas efectivas (regla del cliente)
                foreach (var a in ops.Where(x => x.Fecha.Date <= corte))
                {
                    horasProd += a.HorasReales.HasValue
                        ? CuadrillaReglas.HorasRealesEfectivas(a)
                        : CuadrillaReglas.HorasPlaneadasEfectivas(a);
                }

                for (var d = desde; d <= corte; d = d.AddDays(1))
                {
                    var (horasDia, diasLab) = ConfigPara(configs, t, d);
                    var laboral = d.DayOfWeek != DayOfWeek.Sunday
                                  && !festivos.Contains(d)
                                  && (diasLab == 6 || d.DayOfWeek != DayOfWeek.Saturday);

                    opsPorDia.TryGetValue(d, out var delDia);
                    var tieneOps = delDia != null && delDia.Count > 0;
                    var ausente = ausencias.Contains(d);
                    manualesTecnico.TryGetValue(d, out var manual);

                    string? motivo = null;
                    if (laboral && !ausente && !tieneOps)
                        motivo = disponibles.Contains(d) ? "Disponible sin actividad operativa" : "Sin actividad programada";
                    else if (tieneOps && delDia!.All(CuadrillaReglas.EsLocativa))
                        motivo = "Solo actividades locativas / oficina";

                    // Un día cuenta como disponible si es laboral y el técnico no está ausente, o si trabajó
                    var cuentaDisponible = (laboral && !ausente) || tieneOps;

                    if (manual != null && manual.Tipo == "JUSTIFICADO")
                    {
                        if (motivo != null)
                        {
                            justificados++;
                            diasOut.Add(new CuadrillaDiaImproductivoDto
                            {
                                TecnicoId = t, TecnicoNombre = nombre, Fecha = d, Origen = "JUSTIFICADO",
                                Motivo = motivo + " — justificado: " + manual.Motivo, Justificado = true
                            });
                        }
                        // Día justificado: no cuenta como improductivo ni como disponibilidad sin trabajo
                        if (!tieneOps) cuentaDisponible = false;
                    }
                    else if (manual != null)
                    {
                        improductivos++;
                        cuentaDisponible = true;
                        diasOut.Add(new CuadrillaDiaImproductivoDto
                        {
                            TecnicoId = t, TecnicoNombre = nombre, Fecha = d, Origen = "MANUAL",
                            Motivo = manual.Motivo + (motivo != null ? " (" + motivo + ")" : ""), Justificado = false
                        });
                    }
                    else if (motivo != null)
                    {
                        improductivos++;
                        diasOut.Add(new CuadrillaDiaImproductivoDto
                        {
                            TecnicoId = t, TecnicoNombre = nombre, Fecha = d, Origen = "AUTOMATICO",
                            Motivo = motivo, Justificado = false
                        });
                    }

                    if (cuentaDisponible) horasDisp += horasDia;
                }
            }

            // --- Cumplimiento de programación y visitas (respetan el filtro de proyecto) ---
            var programadas = acts.Where(a => techs.Contains(a.TecnicoId)
                                              && CuadrillaReglas.EsOperativa(a)
                                              && a.Fecha.Date <= corte);
            if (proyectoId.HasValue) programadas = programadas.Where(a => a.ProyectoId == proyectoId.Value);
            var progList = programadas.ToList();

            var ejecutadas = progList.Count(a => a.Estado == "Finalizada");
            var visitas = progList.Where(a => a.TipoRegistro == "VisitaSupervision").ToList();
            var visitasHechas = visitas.Count(a => a.Estado == "Finalizada");

            var bloque = new CuadrillaKpiBloque
            {
                HorasProductivas = Math.Round(horasProd, 1),
                HorasDisponibles = Math.Round(horasDisp, 1),
                DiasImproductivos = improductivos,
                DiasJustificados = justificados
            };

            bloque.Indicadores.Add(Indicador("utilizacion", "Utilización de recursos", horasProd, horasDisp,
                CuadrillaReglas.MetaUtilizacion, CuadrillaReglas.PesoUtilizacion,
                $"{horasProd:0.0} h productivas / {horasDisp:0.0} h disponibles"));
            bloque.Indicadores.Add(Indicador("cumplimiento", "Cumplimiento de programación", ejecutadas, progList.Count,
                CuadrillaReglas.MetaCumplimiento, CuadrillaReglas.PesoCumplimiento,
                $"{ejecutadas} ejecutadas / {progList.Count} programadas"));
            bloque.Indicadores.Add(Indicador("visitas", "Cumplimiento de visitas de supervisión", visitasHechas, visitas.Count,
                CuadrillaReglas.MetaVisitas, CuadrillaReglas.PesoVisitas,
                $"{visitasHechas} realizadas / {visitas.Count} programadas"));
            bloque.Indicadores.Add(new CuadrillaIndicadorDto
            {
                Codigo = "improductivos",
                Titulo = "Días improductivos",
                Valor = improductivos,
                Numerador = improductivos,
                Denominador = 0,
                Meta = null,
                Peso = null,
                TieneDatos = true,
                Cumple = null,
                Formula = $"{improductivos} días sin actividad efectiva" + (justificados > 0 ? $" ({justificados} justificados)" : string.Empty)
            });

            return bloque;
        }

        private static CuadrillaIndicadorDto Indicador(
            string codigo, string titulo, double numerador, double denominador, double meta, double peso, string formula)
        {
            var tiene = denominador > 0;
            double? valor = tiene ? Math.Round(numerador / denominador * 100, 1) : (double?)null;
            return new CuadrillaIndicadorDto
            {
                Codigo = codigo,
                Titulo = titulo,
                Valor = valor,
                Numerador = Math.Round(numerador, 1),
                Denominador = Math.Round(denominador, 1),
                Meta = meta,
                Peso = peso,
                TieneDatos = tiene,
                Cumple = tiene ? valor >= meta : (bool?)null,
                Formula = formula
            };
        }
    }
}
