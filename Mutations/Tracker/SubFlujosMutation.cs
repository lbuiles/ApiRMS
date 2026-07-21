using HotChocolate;
using RmsErp.Api.Data;
using HotChocolate.Authorization;
using System;
using System.Threading.Tasks;
using RmsErp.Api.Models.Tracker;
using Microsoft.EntityFrameworkCore;
using System.Linq; // Necesario para .Where() y .SumAsync()
using System.Net.Http; // Necesario para enviar el Webhook

namespace RmsErp.Api.Mutations.Tracker
{
    // ==========================================
    // DTOs / INPUTS
    // ==========================================

    public record RequisicionInput(
        Guid ProyectoId,
        string Consecutivo,
        decimal? ValorEstimado,
        string? Observaciones,
        Guid SolicitanteId,
        Guid? OrdenTrabajoId  // Opcional: víncula la requisición a una OT interna específica
    );

    public record ComprarRequisicionInput(
        Guid RequisicionId,
        string NumeroOCCompra,
        decimal ValorRealCompra,
        Guid CompradoPorId
    );

    public record RecibirRequisicionInput(
        Guid RequisicionId,
        Guid RecibidoPorId,
        string? ObservacionesRecibo
    );

    public record OrdenTrabajoInput(
        Guid ProyectoId,
        string Consecutivo,
        string? ContratistaNombre,
        string? ContratistaNit,
        Guid? TecnicoInternoId,
        decimal ValorTotal,
        string AlcanceServicio,
        Guid CreadorId
    );

    public record AnticipoInput(
        Guid ProyectoId,
        Guid OrdenTrabajoId,
        decimal ValorAnticipo,
        Guid SolicitanteId
    );

    public record PagarAnticipoInput(
        Guid AnticipoId,
        string IdEgresoWorldOffice
    );

    // ==========================================
    // MUTACIONES
    // ==========================================

    [ExtendObjectType("Mutation")]
    [Authorize]
    public class SubFlujosMutation
    {
        // ------------------------------------------
        // 1. REQUISICIONES
        // ------------------------------------------
        [Authorize(Policy = "proyectos.editar")]
        public async Task<ProyectoRequisicion> AddRequisicion(
            RequisicionInput input,
            [Service] ApplicationDbContext context)
        {
            // Sin candado de estado — disponible en cualquier etapa del proyecto
            var proyecto = await context.Proyectos.FindAsync(input.ProyectoId);
            if (proyecto == null)
                throw new GraphQLException("Proyecto no encontrado.");

            // ==========================================
            // CANDADO DE NEGOCIO: REQUISICIONES SOLO PARA TÉCNICOS INTERNOS
            // Si se vincula a una OT, debe ser una OT interna (técnico empleado).
            // Los contratistas externos reciben anticipos, no requisiciones de materiales.
            // ==========================================
            if (input.OrdenTrabajoId.HasValue)
            {
                var ot = await context.ProyectoOrdenesTrabajo.FindAsync(input.OrdenTrabajoId.Value);
                if (ot == null)
                    throw new GraphQLException("Operación denegada: La Orden de Trabajo vinculada no existe.");

                if (!string.IsNullOrWhiteSpace(ot.ContratistaNombre))
                    throw new GraphQLException("Regla de negocio: Las Requisiciones de materiales son exclusivas para Órdenes de Trabajo de técnicos internos. Los contratistas externos solicitan Anticipos. Verifica que la OT seleccionada esté asignada a un técnico interno.");
            }

            var req = new ProyectoRequisicion
            {
                ProyectoId = input.ProyectoId,
                Consecutivo = input.Consecutivo,
                ValorEstimado = input.ValorEstimado ?? 0,
                Observaciones = input.Observaciones,
                SolicitanteId = input.SolicitanteId,
                Estado = "Por Aprobar Supervisor",
                FechaSolicitud = DateTime.UtcNow
            };

            context.ProyectoRequisiciones.Add(req);
            await context.SaveChangesAsync();
            
            // Nota: Aquí NO recalculamos porque aún está "Por Aprobar". No es un gasto oficial todavía.
            return req;
        }

        [Authorize(Policy = "proyectos.editar")] 
        public async Task<ProyectoRequisicion> AprobarRequisicion(
            Guid id,
            Guid aprobadorId,
            [Service] ApplicationDbContext context)
        {
            var req = await context.ProyectoRequisiciones.FindAsync(id);
            if (req == null) throw new GraphQLException("Requisición no encontrada.");

            // La transición depende del estado actual, no del valor
            if (req.Estado == "Por Aprobar Supervisor")
            {
                // Supervisor aprueba:
                // - Valor <= $1M → directo a En Compras
                // - Valor > $1M  → requiere visto bueno de Gerencia
                if (req.ValorEstimado == 0 || req.ValorEstimado <= 1000000)
                {
                    req.Estado = "En Compras";
                }
                else
                {
                    req.Estado = "Por Aprobar Gerencia";

                    // Notificar a Gerencia vía n8n
                    var proyectoAlerta = await context.Proyectos.FindAsync(req.ProyectoId);
                    if (proyectoAlerta != null)
                    {
                        await NotificarN8n(
                            evento: "ALERTA_GERENCIA_REQ",
                            codigoProyecto: proyectoAlerta.Codigo,
                            monto: req.ValorEstimado,
                            mensaje: $"La requisición {req.Consecutivo} supera el $1.000.000 y requiere aprobación gerencial."
                        );
                    }
                }
            }
            else if (req.Estado == "Por Aprobar Gerencia")
            {
                // Gerencia aprueba → siempre pasa a En Compras
                req.Estado = "En Compras";
            }
            else
            {
                throw new GraphQLException($"Flujo inválido: La requisición no puede ser aprobada desde el estado '{req.Estado}'.");
            }

            req.AprobadorId = aprobadorId;
            req.FechaAprobacion = DateTime.UtcNow;

            await context.SaveChangesAsync();

            // Recalcular finanzas solo cuando ya entró a compras
            if (req.Estado == "En Compras")
            {
                await RecalcularFinanzasProyecto(req.ProyectoId, context);
            }

            return req;
        }

        [Authorize(Policy = "proyectos.editar")]
        // Despacho Interno: aprueba y salta directo a Por Entregar (material propio)
        public async Task<ProyectoRequisicion> DespachoInternoRequisicion(
            Guid id,
            Guid aprobadorId,
            [Service] ApplicationDbContext context)
        {
            var req = await context.ProyectoRequisiciones.FindAsync(id);
            if (req == null) throw new GraphQLException("Requisición no encontrada.");

            if (req.Estado != "Por Aprobar Supervisor" && req.Estado != "Por Aprobar Gerencia")
                throw new GraphQLException($"Solo se puede hacer despacho interno desde estado 'Por Aprobar'. Estado actual: {req.Estado}.");

            req.Estado = "Por Entregar";
            req.AprobadorId = aprobadorId;
            req.FechaAprobacion = DateTime.UtcNow;
            req.NumeroOCCompra = "INVENTARIO INTERNO";

            await context.SaveChangesAsync();
            return req;
        }

        public async Task<ProyectoRequisicion> ComprarRequisicion(
            ComprarRequisicionInput input,
            [Service] ApplicationDbContext context)
        {
            var req = await context.ProyectoRequisiciones.FindAsync(input.RequisicionId);
            if (req == null) throw new GraphQLException("Requisición no encontrada.");

            if (req.Estado != "En Compras")
                throw new GraphQLException($"Flujo inválido: Solo se puede registrar la compra cuando la requisición está 'En Compras'. Estado actual: {req.Estado}.");

            if (string.IsNullOrWhiteSpace(input.NumeroOCCompra))
                throw new GraphQLException("Requisito obligatorio: Debes adjuntar el Número de Orden de Compra (OC) emitida al proveedor.");

            if (input.ValorRealCompra <= 0)
                throw new GraphQLException("Requisito obligatorio: El valor real de la compra debe ser mayor a cero.");

            req.NumeroOCCompra = input.NumeroOCCompra;
            req.ValorRealCompra = input.ValorRealCompra;
            req.CompradoPorId = input.CompradoPorId;
            req.FechaCompra = DateTime.UtcNow;
            req.Estado = "Por Entregar";

            await context.SaveChangesAsync();
            return req;
        }

        [Authorize(Policy = "proyectos.editar")]
        public async Task<ProyectoRequisicion> RecibirRequisicion(
            RecibirRequisicionInput input,
            [Service] ApplicationDbContext context)
        {
            var req = await context.ProyectoRequisiciones.FindAsync(input.RequisicionId);
            if (req == null) throw new GraphQLException("Requisición no encontrada.");

            if (req.Estado != "Por Entregar")
                throw new GraphQLException($"Flujo inválido: Solo se puede confirmar el recibo cuando la requisición está 'Por Entregar'. Estado actual: {req.Estado}.");

            req.RecibidoPorId = input.RecibidoPorId;
            req.FechaRecibo = DateTime.UtcNow;
            req.ObservacionesRecibo = input.ObservacionesRecibo;
            req.Estado = "CERRADA";

            // Recalcular finanzas del proyecto con el valor real de compra
            await RecalcularFinanzasProyecto(req.ProyectoId, context);

            await context.SaveChangesAsync();
            return req;
        }

        // ------------------------------------------
        // 2. ÓRDENES DE TRABAJO (OTs)
        // ------------------------------------------
        [Authorize(Policy = "proyectos.editar")]
        public async Task<ProyectoOrdenTrabajo> AddOrdenTrabajo(
            OrdenTrabajoInput input,
            [Service] ApplicationDbContext context)
        {
            // Candado 0: el proyecto debe estar en ejecución
            var proyecto = await context.Proyectos.FindAsync(input.ProyectoId);
            if (proyecto == null)
                throw new GraphQLException("Proyecto no encontrado.");

            // Sin candado de estado — disponible en cualquier etapa del proyecto

            // Candado 1: debe tener al menos uno
            if (string.IsNullOrWhiteSpace(input.ContratistaNombre) && !input.TecnicoInternoId.HasValue)
                throw new GraphQLException("Error: Debes asignar un Contratista Externo o un Técnico Interno a la OT.");

            // Candado 2: exclusión mutua — no pueden venir los dos al mismo tiempo
            if (!string.IsNullOrWhiteSpace(input.ContratistaNombre) && input.TecnicoInternoId.HasValue)
                throw new GraphQLException("Regla de negocio: Una OT no puede tener simultáneamente un Técnico Interno y un Contratista Externo. Elige solo uno de los dos ejecutores.");

            // Todas las OTs nacen EMITIDAS — el anticipo es independiente por módulo ANT
            var estadoInicial = "EMITIDA";

            var ot = new ProyectoOrdenTrabajo
            {
                ProyectoId = input.ProyectoId,
                Consecutivo = input.Consecutivo,
                ContratistaNombre = input.ContratistaNombre,
                ContratistaNit = input.ContratistaNit,
                TecnicoInternoId = input.TecnicoInternoId, 
                ValorTotal = input.ValorTotal,
                AlcanceServicio = input.AlcanceServicio,
                CreadorId = input.CreadorId,
                Estado = estadoInicial,
                FechaEmision = DateTime.UtcNow
            };

            context.ProyectoOrdenesTrabajo.Add(ot);
            await context.SaveChangesAsync();

            // Siempre recalcular finanzas al emitir la OT
            if (estadoInicial == "EMITIDA")
                await RecalcularFinanzasProyecto(input.ProyectoId, context);
            
            return ot;
        }

        // ------------------------------------------
        // 3. ANTICIPOS
        // ------------------------------------------
        [Authorize(Policy = "proyectos.editar")]
        public async Task<ProyectoAnticipo> AddAnticipo(
            AnticipoInput input,
            [Service] ApplicationDbContext context)
        {
            // 1. Verificamos que la OT exista
            var ot = await context.ProyectoOrdenesTrabajo.FindAsync(input.OrdenTrabajoId);
            if (ot == null) 
                throw new GraphQLException("Operación denegada: La Orden de Trabajo (OT) seleccionada no existe.");

            // ==========================================
            // CANDADO DE NEGOCIO: ANTICIPOS SOLO PARA CONTRATISTAS EXTERNOS
            // Los técnicos internos son empleados — no requieren anticipo de contratista.
            // ==========================================
            if (ot.TecnicoInternoId.HasValue && string.IsNullOrWhiteSpace(ot.ContratistaNombre))
                throw new GraphQLException("Regla de negocio: Los anticipos solo aplican para Órdenes de Trabajo asignadas a contratistas externos. Esta OT está asignada a un técnico interno (empleado). Si necesitas registrar materiales, usa una Requisición en su lugar.");

            // ==========================================
            // CANDADO FINANCIERO: PREVENCIÓN DE SOBREGIRO ACUMULADO
            // ==========================================
            
            var sumaAnticiposPrevios = await context.ProyectoAnticipos
                .Where(a => a.OrdenTrabajoId == input.OrdenTrabajoId && a.Estado != "Rechazado") 
                .SumAsync(a => a.ValorAnticipo);

            var totalProyectado = sumaAnticiposPrevios + input.ValorAnticipo;

            if (totalProyectado > ot.ValorTotal)
            {
                throw new GraphQLException($"Alerta Financiera: El anticipo de {input.ValorAnticipo:C0} supera el cupo. Ya se habían solicitado {sumaAnticiposPrevios:C0}. El tope máximo de la OT es {ot.ValorTotal:C0}.");
            }

            // ==========================================
            // CREACIÓN DEL ANTICIPO
            // ==========================================
            var anticipo = new ProyectoAnticipo
            {
                ProyectoId = input.ProyectoId,
                OrdenTrabajoId = input.OrdenTrabajoId,
                ValorAnticipo = input.ValorAnticipo,
                SolicitanteId = input.SolicitanteId,
                Estado = "SOLICITADO",
                FechaSolicitud = DateTime.UtcNow
            };

            context.ProyectoAnticipos.Add(anticipo);

            // Activamos la OT automáticamente si estaba en borrador
            bool recalcular = false;
            if (ot.Estado == "Borrador")
            {
                ot.Estado = "EMITIDA";
                recalcular = true; // Si la OT se activó, ahora sí cuenta como costo
            }

            await context.SaveChangesAsync();

            // Sincronizamos los totales del proyecto
            if (recalcular)
            {
                await RecalcularFinanzasProyecto(input.ProyectoId, context);
            }

            // ==========================================
            // ALERTA: DISPARAR WEBHOOK A n8n
            // ==========================================
            var proyectoAlerta = await context.Proyectos.FindAsync(input.ProyectoId);
            if (proyectoAlerta != null)
            {
                await NotificarN8n(
                    evento: "ANTICIPO_CREADO",
                    codigoProyecto: proyectoAlerta.Codigo,
                    monto: input.ValorAnticipo,
                    mensaje: $"El líder requiere un anticipo para el contratista {ot.ContratistaNombre} (OT: {ot.Consecutivo})."
                );
            }

            return anticipo;
        }

        [Authorize(Policy = "proyectos.fase.administrativa")] 
        public async Task<ProyectoAnticipo> PagarAnticipo(
            PagarAnticipoInput input,
            [Service] ApplicationDbContext context)
        {
            var anticipo = await context.ProyectoAnticipos.FindAsync(input.AnticipoId);
            if (anticipo == null) throw new GraphQLException("Anticipo no encontrado.");

            if (string.IsNullOrWhiteSpace(input.IdEgresoWorldOffice))
                throw new GraphQLException("Auditoría Contable: Es obligatorio ingresar el ID o Consecutivo del Egreso de WorldOffice para registrar el pago en el ERP.");

            anticipo.IdEgresoWorldOffice = input.IdEgresoWorldOffice;
            anticipo.Estado = "PAGADO";
            anticipo.FechaPago = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return anticipo;
        }

        // ==========================================
        // MOTOR DE SINCRONIZACIÓN FINANCIERA
        // ==========================================
        private async Task RecalcularFinanzasProyecto(Guid proyectoId, ApplicationDbContext context)
        {
            var proyecto = await context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null) return;

            // 1. Sumamos Requisiciones (Oficializadas)
            var totalRequisiciones = await context.ProyectoRequisiciones
                .Where(r => r.ProyectoId == proyectoId && r.Estado != "Por Aprobar Supervisor" && r.Estado != "Rechazado")
                .SumAsync(r => r.ValorEstimado);

            // 2. Sumamos Órdenes de Trabajo (Que ya no son borrador)
            var totalOTs = await context.ProyectoOrdenesTrabajo
                .Where(ot => ot.ProyectoId == proyectoId && ot.Estado != "Borrador")
                .SumAsync(ot => ot.ValorTotal);

            // 3. Sincronizamos con la tabla principal
            proyecto.ValorGasto = totalRequisiciones; // Gastos de materiales/compras
            proyecto.CostoRealTotal = totalOTs;       // Costos de mano de obra/contratistas

            await context.SaveChangesAsync();
        }

        // ==========================================
        // MOTOR DE NOTIFICACIONES (n8n Webhook)
        // ==========================================
        private async Task NotificarN8n(string evento, string codigoProyecto, decimal monto, string mensaje)
        {
            try
            {
                using var client = new HttpClient();
                
                // NOTA: Usamos 'erp-n8n' porque ambos contenedores están en la misma red de Docker (erp-net)
                // Usamos '/webhook/' (producción) y no '/webhook-test/'
                var urlWebhook = "http://erp-n8n:5678/webhook/notificacion-erp"; 

                var payload = new
                {
                    proyecto = codigoProyecto,
                    evento = evento,
                    monto = monto,
                    mensaje = mensaje
                };

                // Convertimos el objeto a JSON
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                // Disparamos la petición de forma asíncrona (fuego y olvido) para no frenar a C#
                await client.PostAsync(urlWebhook, content);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error interno al notificar a n8n: {ex.Message}");
            }
        }
    }

    // ============================================================
    // MUTATIONS: Anticipos Directos (sin OT)
    // ============================================================
    public record AnticipoDirectoInput(
        Guid ProyectoId,
        Guid SolicitanteId,
        decimal ValorAnticipo,
        string Concepto,
        string? Beneficiario,
        string? Observaciones
    );

    public record AprobarAnticipoDirectoInput(
        Guid AnticipoId
    );

    public record PagarAnticipoDirectoInput(
        Guid AnticipoId,
        string IdEgresoWorldOffice
    );

    [ExtendObjectType("Mutation")]
    public class AnticipoDirectoMutation
    {
        [Authorize(Policy = "proyectos.fase.operativa")]
        public async Task<ProyectoAnticipoDirecto> AddAnticipoDirecto(
            AnticipoDirectoInput input,
            [Service] ApplicationDbContext context)
        {
            var proyecto = await context.Proyectos.FindAsync(input.ProyectoId);
            if (proyecto == null)
                throw new GraphQLException("Proyecto no encontrado.");

            var anticipo = new ProyectoAnticipoDirecto
            {
                ProyectoId    = input.ProyectoId,
                SolicitanteId = input.SolicitanteId,
                ValorAnticipo = input.ValorAnticipo,
                Concepto      = input.Concepto,
                Beneficiario  = input.Beneficiario,
                Observaciones = input.Observaciones,
                Estado        = "SOLICITADO"
            };

            context.ProyectoAnticiposDirectos.Add(anticipo);
            await context.SaveChangesAsync();
            return anticipo;
        }

        [Authorize(Policy = "proyectos.fase.administrativa")]
        public async Task<ProyectoAnticipoDirecto> AprobarAnticipoDirecto(
            AprobarAnticipoDirectoInput input,
            [Service] ApplicationDbContext context)
        {
            var anticipo = await context.ProyectoAnticiposDirectos.FindAsync(input.AnticipoId);
            if (anticipo == null) throw new GraphQLException("Anticipo no encontrado.");

            anticipo.Estado          = "APROBADO";
            anticipo.AprobadorId     = null; // Se puede agregar aprobadorId al input si se requiere
            anticipo.FechaAprobacion = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return anticipo;
        }

        [Authorize(Policy = "proyectos.fase.administrativa")]
        public async Task<ProyectoAnticipoDirecto> PagarAnticipoDirecto(
            PagarAnticipoDirectoInput input,
            [Service] ApplicationDbContext context)
        {
            var anticipo = await context.ProyectoAnticiposDirectos.FindAsync(input.AnticipoId);
            if (anticipo == null) throw new GraphQLException("Anticipo no encontrado.");

            anticipo.Estado               = "PAGADO";
            anticipo.IdEgresoWorldOffice   = input.IdEgresoWorldOffice;
            anticipo.FechaPago            = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return anticipo;
        }

        [Authorize(Policy = "proyectos.fase.administrativa")]
        public async Task<bool> RechazarAnticipoDirecto(
            Guid id,
            [Service] ApplicationDbContext context)
        {
            var anticipo = await context.ProyectoAnticiposDirectos.FindAsync(id);
            if (anticipo == null) return false;
            anticipo.Estado = "RECHAZADO";
            await context.SaveChangesAsync();
            return true;
        }
    }
    // ============================================================
    // ELIMINAR OT / REQ / ANT DIRECTO
    // Solo cuando el proyecto no está cerrado
    // ============================================================
    [ExtendObjectType("Mutation")]
    public class EliminarSubFlujosMutation
    {
        private static readonly string[] EstadosCerrados = { "FINALIZADO_TOTAL", "Cancelado", "Incumplimiento" };

        public async Task<bool> EliminarOrdenTrabajo(
            Guid id,
            [Service] ApplicationDbContext context)
        {
            var ot = await context.ProyectoOrdenesTrabajo
                .Include(o => o.Proyecto)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (ot == null)
                throw new GraphQLException("OT no encontrada.");

            if (EstadosCerrados.Contains(ot.Proyecto?.Estado))
                throw new GraphQLException("No se puede eliminar una OT de un proyecto cerrado.");

            context.ProyectoOrdenesTrabajo.Remove(ot);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarRequisicion(
            Guid id,
            [Service] ApplicationDbContext context)
        {
            var req = await context.ProyectoRequisiciones
                .Include(r => r.Proyecto)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (req == null)
                throw new GraphQLException("Requisición no encontrada.");

            if (EstadosCerrados.Contains(req.Proyecto?.Estado))
                throw new GraphQLException("No se puede eliminar una requisición de un proyecto cerrado.");

            context.ProyectoRequisiciones.Remove(req);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EliminarAnticipoDirecto(
            Guid id,
            [Service] ApplicationDbContext context)
        {
            var ant = await context.ProyectoAnticiposDirectos
                .Include(a => a.Proyecto)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (ant == null)
                throw new GraphQLException("Anticipo no encontrado.");

            if (EstadosCerrados.Contains(ant.Proyecto?.Estado))
                throw new GraphQLException("No se puede eliminar un anticipo de un proyecto cerrado.");

            if (ant.Estado == "PAGADO")
                throw new GraphQLException("No se puede eliminar un anticipo ya pagado.");

            context.ProyectoAnticiposDirectos.Remove(ant);
            await context.SaveChangesAsync();
            return true;
        }
    }

}