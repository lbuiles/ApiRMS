using HotChocolate;
using RmsErp.Api.Data;
using HotChocolate.Authorization;
using System;
using System.Threading.Tasks;
using RmsErp.Api.Models.Tracker;
using Microsoft.EntityFrameworkCore;

namespace RmsErp.Api.Mutations.Tracker
{
    // --- DTOs / INPUTS ---
    public record ProyectoInput(
        string LineaNegocio,
        string Codigo,
        string Nombre,
        Guid ClienteId,
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
            var nuevoProyecto = new Proyecto
            {
                LineaNegocio = input.LineaNegocio,
                Codigo = input.Codigo,
                Nombre = input.Nombre,
                ClienteId = input.ClienteId,
                SupervisorCliente = input.SupervisorCliente,
                ResponsableId = input.ResponsableId,
                ValorOC = input.ValorOC,
                FechaAsignacion = input.FechaAsignacion ?? DateTime.UtcNow,
                FechaRespuestaCliente = input.FechaRespuestaCliente,
                Estado = "PENDIENTE" 
            };

            context.Proyectos.Add(nuevoProyecto);
            await context.SaveChangesAsync();
            return nuevoProyecto;
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
            proyecto.Nombre = input.Nombre;
            proyecto.ResponsableId = input.ResponsableId;
            proyecto.SupervisorCliente = input.SupervisorCliente;
            proyecto.Contratista = input.Contratista;
            proyecto.EjecutorInternoId = input.EjecutorInternoId;
            proyecto.Polizas = input.Polizas;
            proyecto.CentroDeCostos = input.CentroDeCostos;
            
            proyecto.NumeroOC = input.NumeroOC;
            proyecto.ValorOC = input.ValorOC;
            proyecto.ValorFacturado = input.ValorFacturado;
            
            // ELIMINADO INTENCIONALMENTE PARA RESPETAR LA ÚNICA FUENTE DE VERDAD:
            // proyecto.ValorGasto = input.ValorGasto; 
            // proyecto.CostoRealTotal = input.CostoRealTotal; 

            // Checklists de cierre
            proyecto.DossierEntregado = input.DossierEntregado;
            proyecto.LiquidacionTerminada = input.LiquidacionTerminada;
            proyecto.FacturacionCompletada = input.FacturacionCompletada;
            proyecto.CierreTecnicoAprobado = input.CierreTecnicoAprobado;

            // Fechas
            proyecto.FechaRespuestaCliente = input.FechaRespuestaCliente;
            proyecto.FechaSolicitudPermisos = input.FechaSolicitudPermisos;
            proyecto.FechaVisitaTecnica = input.FechaVisitaTecnica;
            proyecto.FechaEnvioPermiso = input.FechaEnvioPermiso;
            proyecto.FechaAprobacionPermisos = input.FechaAprobacionPermisos;
            proyecto.FechaEnvioPresupuesto = input.FechaEnvioPresupuesto;
            proyecto.FechaAprobacionPresupuesto = input.FechaAprobacionPresupuesto;
            proyecto.FechaInicioActividades = input.FechaInicioActividades;
            proyecto.FechaTentativaFin = input.FechaTentativaFin;
            proyecto.FechaFinalizacionActividades = input.FechaFinalizacionActividades;

            // ==========================================
            // 2. CANDADOS ADMINISTRATIVOS Y PREVIOS (Reglas Excel)
            // ==========================================
            
            // OC no requiere presupuesto previo — puede registrarse como PENDIENTE o en cualquier momento

            // Reglas de Cierre Administrativo
            if (input.FacturacionCompletada)
            {
                if (string.IsNullOrWhiteSpace(input.NumeroOC))
                    throw new GraphQLException("Bloqueo de Facturación: El proyecto no tiene un Número de OC registrado.");
                if (input.ValorFacturado <= 0)
                    throw new GraphQLException("Bloqueo Financiero: El 'Total Facturado' no puede ser 0.");
            }

            if (input.LiquidacionTerminada && !proyecto.FechaFinalizacionActividades.HasValue)
                throw new GraphQLException("Bloqueo Operativo: No puedes liquidar al contratista si las actividades no han finalizado.");

            if (input.DossierEntregado && !proyecto.FechaFinalizacionActividades.HasValue)
                throw new GraphQLException("Bloqueo Operativo: No puedes entregar el Dossier si las actividades no han finalizado.");

            if (input.LiquidacionTerminada && !proyecto.CierreTecnicoAprobado)
                throw new GraphQLException("Secuencia inválida: No puedes marcar la liquidación como terminada si el 'Cierre Técnico en Terreno' no ha sido aprobado primero.");

            // ==========================================
            // 3. MÁQUINA DE ESTADOS AUTOMÁTICA Y REGLAS DE FLUJO
            // ==========================================

            // Regla: Estados Forzados (Cancelaciones)
            if (!string.IsNullOrEmpty(input.EstadoForzado) && 
               (input.EstadoForzado == "Cancelado" || input.EstadoForzado == "Incumplimiento"))
            {
                proyecto.Estado = input.EstadoForzado;
            }
            // Regla Fila 14: Análisis Final -> FINALIZADO_TOTAL
            else if (proyecto.DossierEntregado && proyecto.LiquidacionTerminada && proyecto.FacturacionCompletada && proyecto.CierreTecnicoAprobado) 
            {
                // Sin candado de gasto — técnicos internos pueden no generar costo registrado
                proyecto.Estado = "FINALIZADO_TOTAL"; 
            }
            // Regla Fila 10: Fin Actividades -> FINALIZADO_PARCIAL
            else if (proyecto.FechaFinalizacionActividades.HasValue) 
            {
                if (!proyecto.FechaInicioActividades.HasValue)
                    throw new GraphQLException("Secuencia inválida: No puedes registrar la fecha de fin de actividades sin haber registrado una fecha de inicio.");
                
                if (proyecto.FechaFinalizacionActividades.Value < proyecto.FechaInicioActividades.Value)
                    throw new GraphQLException("Secuencia inválida: La fecha de finalización no puede ser anterior a la fecha de inicio.");

                // ==========================================
                // CANDADO OPERATIVO: Sub-flujos abiertos al momento de terminar la obra
                // No tiene sentido finalizar si aún hay compras o pagos sin resolver
                // ==========================================

                // 0. Debe existir al menos una OT o una Requisición registrada
                var tieneOTs = await context.ProyectoOrdenesTrabajo
                    .AnyAsync(ot => ot.ProyectoId == id);

                var tieneRequisiciones = await context.ProyectoRequisiciones
                    .AnyAsync(r => r.ProyectoId == id);

                // OTs y REQs opcionales — según Julián no debe ser requisito para finalizar

                // 1. OTs en Borrador (nunca se activaron — posiblemente abandonadas)
                // Solo OTs de contratistas externos pueden quedar en Borrador
                // (las de técnico interno nacen EMITIDAS directamente)
                var otsBorrador = await context.ProyectoOrdenesTrabajo
                    .AnyAsync(ot => ot.ProyectoId == id && ot.Estado == "Borrador" && ot.TecnicoInternoId == null);

                if (otsBorrador)
                    throw new GraphQLException("Candado Operativo: Hay Órdenes de Trabajo en estado 'Borrador'. Debes activarlas (solicitando un anticipo) o eliminarlas antes de registrar el fin de la obra.");

                // 2. Requisiciones que no han completado su ciclo completo
                var reqPendientes = await context.ProyectoRequisiciones
                    .AnyAsync(r => r.ProyectoId == id &&
                                  r.Estado != "CERRADA" &&
                                  r.Estado != "Rechazado");

                if (reqPendientes)
                    throw new GraphQLException("Candado Operativo: Hay Requisiciones que no han completado su ciclo. Deben estar en estado 'CERRADA' antes de registrar el fin de obra.");

                // 3. Anticipos solicitados pero aún sin pagar
                var antPendientes = await context.ProyectoAnticipos
                    .AnyAsync(a => a.ProyectoId == id &&
                                  (a.Estado == "SOLICITADO" || a.Estado == "Por Aprobar"));

                if (antPendientes)
                    throw new GraphQLException("Candado Operativo: Hay Anticipos a contratistas pendientes de pago en WorldOffice. Ciérralos antes de registrar el fin de la obra.");

                proyecto.Estado = "FINALIZADO_PARCIAL"; 
            }
            // Regla Fila 8 y 9: Inicio en Terreno -> EN_EJECUCIÓN
            else if (proyecto.FechaInicioActividades.HasValue)
            {
                // Ejecutor ya no es requerido aquí — se asigna desde la OT
                // Candado de Fila 8: Pólizas aprobadas
                if (string.IsNullOrWhiteSpace(input.Polizas) || input.Polizas == "Requiere - Pendiente")
                    throw new GraphQLException("Requisito previo: Debes definir y aprobar el estado de las pólizas antes de autorizar el inicio de actividades en terreno.");

                proyecto.Estado = "EN_EJECUCIÓN"; 
            }
            // Regla Fila 2: CentroDeCostos -> PENDIENTE_PRELIMINARES (segundo paso del flujo)
            // También activa con cualquier otro dato de preliminares ingresado
            else if (!string.IsNullOrWhiteSpace(input.CentroDeCostos) ||
                     proyecto.FechaRespuestaCliente.HasValue ||
                     proyecto.FechaAprobacionPresupuesto.HasValue ||
                     proyecto.FechaEnvioPresupuesto.HasValue ||
                     proyecto.FechaSolicitudPermisos.HasValue ||
                     !string.IsNullOrWhiteSpace(input.NumeroOC))
            {
                // El Centro de Costos es el primer requisito de preliminares
                // Si hay datos de fases más avanzadas pero falta el CC, lo advertimos
                if (string.IsNullOrWhiteSpace(input.CentroDeCostos) &&
                    (proyecto.FechaSolicitudPermisos.HasValue || proyecto.FechaEnvioPresupuesto.HasValue))
                    throw new GraphQLException("Requisito previo: Debes registrar el Centro de Costos (ID de WorldOffice) antes de avanzar a la fase de permisos y presupuesto.");

                proyecto.Estado = "PENDIENTE_PRELIMINARES";
            }
            // Estado por defecto (Nacimiento del proyecto - Fila 1)
            else
            {
                proyecto.Estado = "PENDIENTE"; 
            }

            // ==========================================
            // 4. AUDITORÍA FINAL DE CALIDAD Y SUB-FLUJOS
            // ==========================================
            if (proyecto.Estado == "FINALIZADO_TOTAL")
            {
                // Candados de campos obligatorios
                if (string.IsNullOrWhiteSpace(input.CentroDeCostos))
                    throw new GraphQLException("Auditoría fallida: El Centro de Costos (ID WorldOffice) es obligatorio para el cierre contable del proyecto. Es el primer requisito del flujo preliminar.");

                if (!input.FechaRespuestaCliente.HasValue)
                    throw new GraphQLException("Auditoría fallida: Es obligatorio registrar la 'Respuesta a Cliente (SLA)' antes de cerrar el proyecto.");
                
                // Permisos opcionales: solo valida si el proyecto tiene alguna fecha de permisos registrada
                // Si ninguna fecha está, significa que no requiere permisos (obra nueva / cliente directo)
                bool tieneAlgunaFechaPermiso = input.FechaSolicitudPermisos.HasValue
                    || input.FechaVisitaTecnica.HasValue
                    || input.FechaAprobacionPermisos.HasValue;
                if (tieneAlgunaFechaPermiso &&
                    (!input.FechaSolicitudPermisos.HasValue || !input.FechaVisitaTecnica.HasValue || !input.FechaAprobacionPermisos.HasValue))
                    throw new GraphQLException("Auditoría fallida: El proyecto tiene fechas de permisos parciales. Completa la solicitud, visita técnica y aprobación — o limpia todas las fechas si no requiere permisos.");

                if (string.IsNullOrWhiteSpace(input.Polizas))
                    throw new GraphQLException("Auditoría fallida: Debes definir el Estado de Pólizas (Requiere, No requiere, etc.) antes de cerrar.");

                if (string.IsNullOrWhiteSpace(input.CentroDeCostos))
                    throw new GraphQLException("Auditoría fallida: El Centro de Costos es obligatorio para el cierre contable del proyecto.");

                // ==========================================
                // CANDADOS DE COMPRAS Y DINERO
                // ==========================================
                
                // 1. Validar que todas las Requisiciones estén CERRADAS
                var reqPendientes = await context.ProyectoRequisiciones
                    .AnyAsync(r => r.ProyectoId == id &&
                                  r.Estado != "CERRADA" &&
                                  r.Estado != "Rechazada");
                
                if (reqPendientes)
                    throw new GraphQLException("Candado Financiero: No puedes cerrar el proyecto porque hay Requisiciones que no han completado su ciclo completo (aprobación → compra → recibo). Todas deben estar en estado 'CERRADA'.");

                // 2. Validar Anticipos Pendientes
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

                // 1. SLA vencido — respuesta al cliente con más de 1 día de retraso
                if (proyecto.FechaRespuestaCliente.HasValue && proyecto.FechaAsignacion != default)
                {
                    var diasSLA = (proyecto.FechaRespuestaCliente.Value.Date - proyecto.FechaAsignacion.Date).Days;
                    if (diasSLA > 1)
                        alertasCriticas.Add($"SLA vencido — respuesta al cliente con {diasSLA} día(s) de retraso");
                }

                // 2. Inicio de obra sin OC registrada
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

                // Datos adicionales para el email
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

                await NotificarAlertaCriticaN8n(
                    proyecto.Codigo, proyecto.Nombre, proyecto.Estado,
                    cliente, responsable, alertasCriticas
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al detectar alertas críticas: {ex.Message}");
            }
        }

        private async Task NotificarAlertaCriticaN8n(
            string codigo, string nombre, string estado,
            string cliente, string responsable, List<string> alertas)
        {
            try
            {
                using var httpClient = new HttpClient();
                var urlWebhook = "http://erp-n8n:5678/webhook/alerta-critica-proyecto";

                var payload = new
                {
                    codigo      = codigo,
                    nombre      = nombre,
                    estado      = estado,
                    cliente     = cliente,
                    responsable = responsable,
                    alertas     = alertas,
                    timestamp   = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
                };

                var json    = System.Text.Json.JsonSerializer.Serialize(payload);
                var body    = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                await httpClient.PostAsync(urlWebhook, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al notificar alerta crítica a n8n: {ex.Message}");
            }
        }

        // La bitácora puede ser registrada por cualquier usuario con acceso
        // a cualquiera de las tres líneas de negocio (leer es suficiente).
        // Se usa proyectos.energia.leer como política base — el PermissionPolicyProvider
        // acepta cualquiera de los tres slugs de línea vía la configuración OR del Program.cs.
        [Authorize(Policy = "proyectos.bitacora")]
        public async Task<ProyectoObservacion?> AddObservacion(
            ObservacionInput input, 
            [Service] ApplicationDbContext context)
        {
            var proyecto = await context.Proyectos.FindAsync(input.ProyectoId);
            if (proyecto == null) return null;

            var nuevaObs = new ProyectoObservacion
            {
                ProyectoId = input.ProyectoId,
                UsuarioId = input.UsuarioId,
                Observacion = input.ObservacionTexto,
                FechaRegistro = DateTime.UtcNow
            };

            context.ProyectoObservaciones.Add(nuevaObs);
            await context.SaveChangesAsync();
            return nuevaObs;
        }
    }
}