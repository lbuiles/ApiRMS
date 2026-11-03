using HotChocolate;
using RmsErp.Api.Data;
using HotChocolate.Authorization;
using System;
using System.Threading.Tasks;
using RmsErp.Api.Models.Tracker;
using RmsErp.Api.Models.Clientes;
using Microsoft.EntityFrameworkCore;

namespace RmsErp.Api.Mutations.Tracker
{
    // --- DTOs / INPUTS ---
    public record ProyectoInput(
        string LineaNegocio,
        string Codigo,
        string Nombre,
        Guid? ClienteId,
        string? ClienteNombre,
        string? SupervisorCliente,
        Guid? ResponsableId,
        decimal ValorOC,
        DateTime? FechaAsignacion,
        DateTime? FechaRespuestaCliente
    );

    public record ProyectoUpdateInput(
        string Nombre,
        Guid? ResponsableId,
        string? SupervisorCliente,
        DateTime? FechaRespuestaCliente,
        string? Contratista,
        Guid? EjecutorInternoId,
        string? Polizas,
        string? CentroDeCostos,
        DateTime? FechaSolicitudPermisos,
        DateTime? FechaVisitaTecnica,
        DateTime? FechaEnvioPermiso,
        DateTime? FechaAprobacionPermisos,
        DateTime? FechaEnvioPresupuesto,
        DateTime? FechaAprobacionPresupuesto,
        DateTime? FechaInicioActividades,
        DateTime? FechaInicioObra,
        DateTime? FechaTentativaFin,
        DateTime? FechaFinalizacionActividades,

        // Checklists
        bool DossierEntregado,
        bool LiquidacionTerminada,
        bool FacturacionCompletada,
        bool CierreTecnicoAprobado,

        // Financiero
        string? NumeroOC,
        decimal ValorOC,
        decimal ValorGasto,
        decimal ValorFacturado,
        decimal CostoRealTotal,

        string? EstadoForzado
    );

    public record ObservacionInput(
        Guid ProyectoId,
        Guid UsuarioId,
        string ObservacionTexto
    );

    [ExtendObjectType("Mutation")]
    [Authorize]
    public class ProyectoMutation
    {
        [Authorize(Policy = "proyectos.crear")]
        public async Task<Proyecto> AddProyecto(
            ProyectoInput input,
            [Service] ApplicationDbContext context)
        {
            // ─── Resolver ClienteId ───────────────────────────────────────────────
            // El Angular puede enviar el UUID (selector) o un nombre libre (texto libre).
            // En modo texto libre, ClienteId llega nulo y ClienteNombre trae la razón social.
            Guid resolvedClienteId;

            if (input.ClienteId.HasValue && input.ClienteId.Value != Guid.Empty)
            {
                resolvedClienteId = input.ClienteId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(input.ClienteNombre))
            {
                var nombre = input.ClienteNombre.Trim();

                // Buscar cliente existente por RazonSocial (insensible a mayúsculas)
                var clienteExistente = await context.Clientes
                    .FirstOrDefaultAsync(c => c.RazonSocial.ToLower() == nombre.ToLower());

                if (clienteExistente != null)
                {
                    resolvedClienteId = clienteExistente.Id;
                }
                else
                {
                    // Crear cliente mínimo con el nombre provisto
                    var nuevoCliente = new Cliente
                    {
                        RazonSocial      = nombre,
                        Nit              = "N/A",
                        UsuarioCreacion  = "SISTEMA"
                    };
                    context.Clientes.Add(nuevoCliente);
                    await context.SaveChangesAsync();
                    resolvedClienteId = nuevoCliente.Id;
                }
            }
            else
            {
                throw new GraphQLException("Se requiere clienteId o clienteNombre para crear un proyecto.");
            }
            // ─────────────────────────────────────────────────────────────────────

            var nuevoProyecto = new Proyecto
            {
                LineaNegocio         = input.LineaNegocio,
                Codigo               = input.Codigo,
                Nombre               = input.Nombre,
                ClienteId            = resolvedClienteId,
                SupervisorCliente    = input.SupervisorCliente,
                ResponsableId        = input.ResponsableId,
                ValorOC              = input.ValorOC,
                FechaAsignacion      = input.FechaAsignacion ?? DateTime.UtcNow,
                FechaRespuestaCliente = input.FechaRespuestaCliente,
                Estado               = "PENDIENTE"
            };

            context.Proyectos.Add(nuevoProyecto);
            await context.SaveChangesAsync();
            return nuevoProyecto;
        }

        /// <summary>
        /// Actualiza únicamente el Nombre del proyecto, sin depender de la validez
        /// del resto de ProyectoUpdateInput (usado por el botón rápido "Editable"
        /// en Identificación del Proyecto). Requiere el mismo permiso que editar.
        /// </summary>
        [Authorize(Policy = "proyectos.editar")]
        public async Task<Proyecto?> ActualizarNombreProyecto(
            Guid id,
            string nombre,
            [Service] ApplicationDbContext context)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new GraphQLException("El nombre del proyecto no puede quedar vacío.");

            var proyecto = await context.Proyectos.FindAsync(id);
            if (proyecto == null) return null;

            proyecto.Nombre = nombre.Trim();
            await context.SaveChangesAsync();
            return proyecto;
        }

        [Authorize(Policy = "proyectos.editar")]
        public async Task<Proyecto?> UpdateProyecto(
            Guid id,
            ProyectoUpdateInput input,
            [Service] ApplicationDbContext context)
        {
            var proyecto = await context.Proyectos.FindAsync(id);
            if (proyecto == null) return null;

            // ==========================================
            // 1. ACTUALIZACIÓN DE DATOS BÁSICOS
            // ==========================================
            proyecto.Nombre               = input.Nombre;
            proyecto.ResponsableId        = input.ResponsableId;
            proyecto.SupervisorCliente    = input.SupervisorCliente;
            proyecto.Contratista          = input.Contratista;
            proyecto.EjecutorInternoId    = input.EjecutorInternoId;
            proyecto.Polizas              = input.Polizas;
            proyecto.CentroDeCostos       = input.CentroDeCostos;

            proyecto.NumeroOC             = input.NumeroOC;
            proyecto.ValorOC              = input.ValorOC;
            proyecto.ValorFacturado       = input.ValorFacturado;

            // Gastos REQ y Costos OTs editables manualmente (solicitado en revisión)
            proyecto.ValorGasto     = input.ValorGasto;
            proyecto.CostoRealTotal = input.CostoRealTotal;

            // Checklists de cierre
            proyecto.DossierEntregado       = input.DossierEntregado;
            proyecto.LiquidacionTerminada   = input.LiquidacionTerminada;
            proyecto.FacturacionCompletada  = input.FacturacionCompletada;
            proyecto.CierreTecnicoAprobado  = input.CierreTecnicoAprobado;

            // Fechas
            proyecto.FechaRespuestaCliente      = input.FechaRespuestaCliente;
            proyecto.FechaSolicitudPermisos     = input.FechaSolicitudPermisos;
            proyecto.FechaVisitaTecnica         = input.FechaVisitaTecnica;
            proyecto.FechaEnvioPermiso          = input.FechaEnvioPermiso;
            proyecto.FechaAprobacionPermisos    = input.FechaAprobacionPermisos;
            proyecto.FechaEnvioPresupuesto      = input.FechaEnvioPresupuesto;
            proyecto.FechaAprobacionPresupuesto = input.FechaAprobacionPresupuesto;
            proyecto.FechaInicioActividades       = input.FechaInicioActividades;
            proyecto.FechaInicioObra              = input.FechaInicioObra;
            proyecto.FechaTentativaFin            = input.FechaTentativaFin;
            proyecto.FechaFinalizacionActividades = input.FechaFinalizacionActividades;

            // ==========================================
            // 2. CANDADOS ADMINISTRATIVOS Y PREVIOS
            // ==========================================

            if (input.FacturacionCompletada)
            {
                if (string.IsNullOrWhiteSpace(input.NumeroOC))
                    throw new GraphQLException("Bloqueo de Facturación: El proyecto no tiene un Número de OC registrado.");
                if (input.ValorFacturado <= 0)
                    throw new GraphQLException("Bloqueo Financiero: El 'Total Facturado' no puede ser 0.");
            }

            // Cierres administrativos independientes — no requieren fecha de fin de obra
            // (Solicitado por Jaime Rojas: los ítems documentales pueden cerrarse en paralelo)

            // ==========================================
            // 3. MÁQUINA DE ESTADOS AUTOMÁTICA
            // ==========================================

            if (!string.IsNullOrEmpty(input.EstadoForzado) &&
               (input.EstadoForzado == "Cancelado" || input.EstadoForzado == "Incumplimiento"))
            {
                proyecto.Estado = input.EstadoForzado;
            }
            else if (proyecto.DossierEntregado && proyecto.LiquidacionTerminada &&
                     proyecto.FacturacionCompletada && proyecto.CierreTecnicoAprobado)
            {
                proyecto.Estado = "FINALIZADO_TOTAL";
            }
            else if (proyecto.FechaFinalizacionActividades.HasValue)
            {
                // Validación de secuencia flexible: se registra aunque no tenga inicio formal
                // (puede haberse iniciado sin autorización digital)

                var otsBorrador = await context.ProyectoOrdenesTrabajo
                    .AnyAsync(ot => ot.ProyectoId == id && ot.Estado == "Borrador" && ot.TecnicoInternoId == null);

                if (otsBorrador)
                    throw new GraphQLException("Candado Operativo: Hay Órdenes de Trabajo en estado 'Borrador'. Debes activarlas o eliminarlas antes de registrar el fin de la obra.");

                var reqPendientes = await context.ProyectoRequisiciones
                    .AnyAsync(r => r.ProyectoId == id &&
                                   r.Estado != "CERRADA" &&
                                   r.Estado != "Rechazado");

                if (reqPendientes)
                    throw new GraphQLException("Candado Operativo: Hay Requisiciones que no han completado su ciclo. Deben estar en estado 'CERRADA' antes de registrar el fin de obra.");

                var antPendientes = await context.ProyectoAnticipos
                    .AnyAsync(a => a.ProyectoId == id &&
                                  (a.Estado == "SOLICITADO" || a.Estado == "Por Aprobar"));

                if (antPendientes)
                    throw new GraphQLException("Candado Operativo: Hay Anticipos a contratistas pendientes de pago en WorldOffice. Ciérralos antes de registrar el fin de la obra.");

                proyecto.Estado = "FINALIZADO_PARCIAL";
            }
            else if (proyecto.FechaInicioActividades.HasValue)
            {
                // Pólizas: ya no bloquean el inicio — se registran como advertencia en el tracker
                proyecto.Estado = "EN_EJECUCIÓN";
            }
            else if (!string.IsNullOrWhiteSpace(input.CentroDeCostos) ||
                     proyecto.FechaRespuestaCliente.HasValue ||
                     proyecto.FechaAprobacionPresupuesto.HasValue ||
                     proyecto.FechaEnvioPresupuesto.HasValue ||
                     proyecto.FechaSolicitudPermisos.HasValue ||
                     !string.IsNullOrWhiteSpace(input.NumeroOC))
            {
                if (string.IsNullOrWhiteSpace(input.CentroDeCostos) &&
                    (proyecto.FechaSolicitudPermisos.HasValue || proyecto.FechaEnvioPresupuesto.HasValue))
                    throw new GraphQLException("Requisito previo: Debes registrar el Centro de Costos antes de avanzar a la fase de permisos y presupuesto.");

                proyecto.Estado = "PENDIENTE_PRELIMINARES";
            }
            else
            {
                proyecto.Estado = "PENDIENTE";
            }

            // ==========================================
            // 4. AUDITORÍA FINAL DE CALIDAD
            // ==========================================
            if (proyecto.Estado == "FINALIZADO_TOTAL")
            {
                if (string.IsNullOrWhiteSpace(input.CentroDeCostos))
                    throw new GraphQLException("Auditoría fallida: El Centro de Costos (ID WorldOffice) es obligatorio para el cierre contable del proyecto.");

                if (!input.FechaRespuestaCliente.HasValue)
                    throw new GraphQLException("Auditoría fallida: Es obligatorio registrar la 'Respuesta a Cliente (SLA)' antes de cerrar el proyecto.");

                bool tieneAlgunaFechaPermiso = input.FechaSolicitudPermisos.HasValue
                    || input.FechaVisitaTecnica.HasValue
                    || input.FechaAprobacionPermisos.HasValue;

                if (tieneAlgunaFechaPermiso &&
                    (!input.FechaSolicitudPermisos.HasValue || !input.FechaVisitaTecnica.HasValue || !input.FechaAprobacionPermisos.HasValue))
                    throw new GraphQLException("Auditoría fallida: El proyecto tiene fechas de permisos parciales. Completa la solicitud, visita técnica y aprobación — o limpia todas las fechas si no requiere permisos.");

                if (string.IsNullOrWhiteSpace(input.Polizas))
                    throw new GraphQLException("Auditoría fallida: Debes definir el Estado de Pólizas antes de cerrar.");

                var reqPendientes = await context.ProyectoRequisiciones
                    .AnyAsync(r => r.ProyectoId == id &&
                                   r.Estado != "CERRADA" &&
                                   r.Estado != "Rechazada");

                if (reqPendientes)
                    throw new GraphQLException("Candado Financiero: No puedes cerrar el proyecto porque hay Requisiciones que no han completado su ciclo completo.");

                var antPendientes = await context.ProyectoAnticipos
                    .AnyAsync(a => a.ProyectoId == id &&
                                  (a.Estado == "SOLICITADO" || a.Estado == "Por Aprobar"));

                if (antPendientes)
                    throw new GraphQLException("Candado Financiero: No puedes finalizar el proyecto porque hay Anticipos a contratistas esperando pago en WorldOffice.");
            }

            await context.SaveChangesAsync();

            // ── Detección automática de alertas críticas ──────────────────
            await DetectarYNotificarAlertasCriticas(proyecto, context);

            return proyecto;
        }

        private async Task DetectarYNotificarAlertasCriticas(Proyecto proyecto, ApplicationDbContext context)
        {
            try
            {
                var estadosActivos = new[] { "PENDIENTE", "PENDIENTE_PRELIMINARES", "EN_EJECUCIÓN", "FINALIZADO_PARCIAL" };
                if (!estadosActivos.Contains(proyecto.Estado)) return;

                var hoy = DateTime.UtcNow.Date;
                var alertasCriticas = new List<string>();

                // 1. SLA vencido
                if (proyecto.FechaRespuestaCliente.HasValue && proyecto.FechaAsignacion != default)
                {
                    var diasSLA = (proyecto.FechaRespuestaCliente.Value.Date - proyecto.FechaAsignacion.Date).Days;
                    if (diasSLA > 1)
                        alertasCriticas.Add($"SLA vencido — {diasSLA} día(s) de retraso (Asignación: {proyecto.FechaAsignacion.Date:dd/MM/yyyy} → Respuesta cliente: {proyecto.FechaRespuestaCliente.Value.Date:dd/MM/yyyy})");
                }

                // 2. Inicio de obra sin OC
                if (proyecto.FechaInicioActividades.HasValue && string.IsNullOrWhiteSpace(proyecto.NumeroOC))
                    alertasCriticas.Add("Inicio de obra registrado sin OC del cliente");

                // 3. Fin de obra vencido
                if (proyecto.Estado == "EN_EJECUCIÓN" && proyecto.FechaFinalizacionActividades.HasValue)
                {
                    var diasVencido = (hoy - proyecto.FechaFinalizacionActividades.Value.Date).Days;
                    if (diasVencido > 0)
                        alertasCriticas.Add($"Fin de obra vencido hace {diasVencido} día(s)");
                }

                if (!alertasCriticas.Any()) return;

                // ── Datos adicionales ─────────────────────────────────────
                var cliente = await context.Clientes
                    .Where(c => c.Id == proyecto.ClienteId)
                    .Select(c => c.RazonSocial)
                    .FirstOrDefaultAsync() ?? "Sin cliente";

                var responsable = proyecto.ResponsableId.HasValue
                    ? await context.Usuarios
                        .Where(u => u.Id == proyecto.ResponsableId)
                        .Select(u => u.Nombre)
                        .FirstOrDefaultAsync() ?? "Sin responsable"
                    : "Sin responsable";

                // ── Destinatarios dinámicos ───────────────────────────────

                // 1. Usuarios con proyectos.gerente — reciben todas las alertas
                var emailsGerentes = await context.UsuarioPermisos
                    .Where(up => up.Permiso.Slug == "proyectos.gerente" &&
                                 up.Usuario.Estado == "ACTIVO")
                    .Select(up => up.Usuario.Email)
                    .ToListAsync();

                // 2. Usuarios con permiso de alerta de la división específica
                var slugDivision = proyecto.LineaNegocio switch
                {
                    "civiles"            => "proyectos.civiles.alertas",
                    "energia"            => "proyectos.energia.alertas",
                    "telecomunicaciones" => "proyectos.telecom.alertas",
                    _                    => null
                };

                var emailsDivision = new List<string>();
                if (slugDivision != null)
                {
                    emailsDivision = await context.UsuarioPermisos
                        .Where(up => up.Permiso.Slug == slugDivision &&
                                     up.Usuario.Estado == "ACTIVO")
                        .Select(up => up.Usuario.Email)
                        .ToListAsync();
                }

                // 3. Deduplicar
                var destinatarios = emailsGerentes
                    .Union(emailsDivision)
                    .Where(e => !string.IsNullOrEmpty(e))
                    .Distinct()
                    .ToList();

                // 4. Nombre legible de la división
                var divisionLabel = proyecto.LineaNegocio switch
                {
                    "civiles"            => "Construcción",
                    "energia"            => "Energía",
                    "telecomunicaciones" => "O&M Telecomunicaciones",
                    _                    => proyecto.LineaNegocio
                };

                await NotificarAlertaCriticaN8n(
                    proyecto.Codigo, proyecto.Nombre, proyecto.Estado,
                    cliente, responsable, divisionLabel,
                    alertasCriticas, destinatarios
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al detectar alertas críticas: {ex.Message}");
            }
        }

        private async Task NotificarAlertaCriticaN8n(
            string codigo, string nombre, string estado,
            string cliente, string responsable,
            string division, List<string> alertas, List<string> destinatarios)
        {
            try
            {
                if (!destinatarios.Any()) return;

                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var urlWebhook = "http://erp-n8n:5678/webhook/alerta-critica-proyecto";

                var payload = new
                {
                    codigo        = codigo,
                    nombre        = nombre,
                    estado        = estado,
                    cliente       = cliente,
                    responsable   = responsable,
                    division      = division,
                    alertas       = alertas,
                    destinatarios = destinatarios,
                    timestamp     = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                };

                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var body = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                await httpClient.PostAsync(urlWebhook, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al notificar alerta crítica a n8n: {ex.Message}");
            }
        }

        [Authorize(Policy = "proyectos.bitacora")]
        public async Task<ProyectoObservacion?> AddObservacion(
            ObservacionInput input,
            [Service] ApplicationDbContext context)
        {
            var proyecto = await context.Proyectos.FindAsync(input.ProyectoId);
            if (proyecto == null) return null;

            var nuevaObs = new ProyectoObservacion
            {
                ProyectoId    = input.ProyectoId,
                UsuarioId     = input.UsuarioId,
                Observacion   = input.ObservacionTexto,
                FechaRegistro = DateTime.UtcNow
            };

            context.ProyectoObservaciones.Add(nuevaObs);
            await context.SaveChangesAsync();

            // Recargar con el Usuario incluido — nuevaObs.Usuario nunca se asigna
            // explícitamente al crear, y sin este Include el campo puede llegar
            // nulo al frontend (mostrando "Usuario" en vez del nombre real).
            return await context.ProyectoObservaciones
                .Include(o => o.Usuario)
                .FirstOrDefaultAsync(o => o.Id == nuevaObs.Id);
        }

        // ==========================================
        // ARCHIVAR / REACTIVAR / ELIMINAR (punto 17)
        // ==========================================

        /// <summary>
        /// Archiva (anula) un proyecto sin perder su historial. Es la operación
        /// por defecto para "quitar" un proyecto de la vista activa.
        /// </summary>
        [Authorize(Policy = "proyectos.borrar")]
        public async Task<Proyecto?> ArchivarProyecto(
            Guid id,
            Guid usuarioId,
            [Service] ApplicationDbContext context)
        {
            var proyecto = await context.Proyectos.FindAsync(id);
            if (proyecto == null) return null;

            proyecto.Archivado = true;
            proyecto.FechaArchivado = DateTime.UtcNow;
            proyecto.ArchivadoPorId = usuarioId;

            await context.SaveChangesAsync();
            return proyecto;
        }

        /// <summary>
        /// Reactiva un proyecto previamente archivado.
        /// </summary>
        [Authorize(Policy = "proyectos.borrar")]
        public async Task<Proyecto?> ReactivarProyecto(
            Guid id,
            [Service] ApplicationDbContext context)
        {
            var proyecto = await context.Proyectos.FindAsync(id);
            if (proyecto == null) return null;

            proyecto.Archivado = false;
            proyecto.FechaArchivado = null;
            proyecto.ArchivadoPorId = null;

            await context.SaveChangesAsync();
            return proyecto;
        }

        /// <summary>
        /// Elimina definitivamente un proyecto. Solo permitido si no tiene
        /// ningún movimiento asociado (OTs, Requisiciones, Anticipos, Anticipos
        /// Directos, facturación o costos registrados) — para proyectos creados
        /// por error. En cualquier otro caso, se debe usar ArchivarProyecto.
        /// </summary>
        [Authorize(Policy = "proyectos.borrar")]
        public async Task<bool> EliminarProyectoDefinitivo(
            Guid id,
            [Service] ApplicationDbContext context)
        {
            var proyecto = await context.Proyectos.FindAsync(id);
            if (proyecto == null) return false;

            var tieneOTs           = await context.ProyectoOrdenesTrabajo.AnyAsync(o => o.ProyectoId == id);
            var tieneRequisiciones = await context.ProyectoRequisiciones.AnyAsync(r => r.ProyectoId == id);
            var tieneAnticipos     = await context.ProyectoAnticipos.AnyAsync(a => a.ProyectoId == id);
            var tieneAnticiposDir  = await context.ProyectoAnticiposDirectos.AnyAsync(a => a.ProyectoId == id);
            var tieneCortesFacturacion = await context.ProyectoCortesFacturacion.AnyAsync(cf => cf.ProyectoId == id);
            var tieneMovimientosFinancieros =
                proyecto.ValorFacturado != 0 || proyecto.ValorGasto != 0 || proyecto.CostoRealTotal != 0;

            if (tieneOTs || tieneRequisiciones || tieneAnticipos || tieneAnticiposDir || tieneCortesFacturacion || tieneMovimientosFinancieros)
            {
                throw new GraphQLException(
                    "Este proyecto ya tiene movimientos asociados (OTs, requisiciones, anticipos o valores financieros). " +
                    "No se puede eliminar definitivamente — solo se puede archivar.");
            }

            context.Proyectos.Remove(proyecto);
            await context.SaveChangesAsync();
            return true;
        }
    }
}