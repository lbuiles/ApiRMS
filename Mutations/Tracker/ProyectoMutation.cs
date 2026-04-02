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
        string? SupervisorCliente, // <-- NUEVO
        Guid? ResponsableId,
        decimal ValorOC
    );

    public record ProyectoUpdateInput(
        string Nombre,
        Guid? ResponsableId,
        string? SupervisorCliente,
        DateTime? FechaRespuestaCliente,
        string? Contratista,
        string? Polizas,
        string? CentroDeCostos,
        DateTime? FechaSolicitudPermisos,
        DateTime? FechaEnvioPermiso,
        DateTime? FechaAprobacionPermisos,
        DateTime? FechaEnvioPresupuesto,
        DateTime? FechaAprobacionPresupuesto,
        DateTime? FechaInicioActividades,
        DateTime? FechaFinalizacionActividades,
        bool DossierEntregado,
        bool LiquidacionTerminada,
        bool FacturacionCompletada,
        string? NumeroOC,
        decimal ValorOC,
        decimal ValorGasto,
        decimal ValorFacturado,
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
                SupervisorCliente = input.SupervisorCliente, // <-- Asignamos el nuevo campo
                ResponsableId = input.ResponsableId,
                ValorOC = input.ValorOC,
                FechaAsignacion = DateTime.UtcNow,
                Estado = "Pendiente" 
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

            // 1. Actualización de datos básicos y administrativos
            proyecto.Nombre = input.Nombre;
            proyecto.ResponsableId = input.ResponsableId;
            proyecto.SupervisorCliente = input.SupervisorCliente;
            proyecto.Contratista = input.Contratista;
            proyecto.Polizas = input.Polizas;
            proyecto.CentroDeCostos = input.CentroDeCostos;
            
            proyecto.NumeroOC = input.NumeroOC;
            proyecto.ValorOC = input.ValorOC;
            proyecto.ValorGasto = input.ValorGasto;
            proyecto.ValorFacturado = input.ValorFacturado;

            // Checklists de cierre
            proyecto.DossierEntregado = input.DossierEntregado;
            proyecto.LiquidacionTerminada = input.LiquidacionTerminada;
            proyecto.FacturacionCompletada = input.FacturacionCompletada;

            // 2. Actualización de Fechas
            proyecto.FechaRespuestaCliente = input.FechaRespuestaCliente;
            proyecto.FechaSolicitudPermisos = input.FechaSolicitudPermisos;
            proyecto.FechaEnvioPermiso = input.FechaEnvioPermiso;
            proyecto.FechaAprobacionPermisos = input.FechaAprobacionPermisos;
            proyecto.FechaEnvioPresupuesto = input.FechaEnvioPresupuesto;
            proyecto.FechaAprobacionPresupuesto = input.FechaAprobacionPresupuesto;
            proyecto.FechaInicioActividades = input.FechaInicioActividades;
            proyecto.FechaFinalizacionActividades = input.FechaFinalizacionActividades;
            // ==========================================
            // 3. CANDADOS CRONOLÓGICOS (Reglas del Flujograma)
            // ==========================================
            if (proyecto.FechaFinalizacionActividades.HasValue && !proyecto.FechaInicioActividades.HasValue)
                throw new GraphQLException("No puedes registrar la finalización de actividades sin haber registrado una fecha de inicio.");

            if (proyecto.FechaFinalizacionActividades.HasValue && proyecto.FechaInicioActividades.HasValue)
            {
                if (proyecto.FechaFinalizacionActividades.Value < proyecto.FechaInicioActividades.Value)
                    throw new GraphQLException("La fecha de finalización no puede ser anterior a la fecha de inicio.");
            }

            if ((input.DossierEntregado || input.LiquidacionTerminada || input.FacturacionCompletada) && !proyecto.FechaFinalizacionActividades.HasValue)
                throw new GraphQLException("Operación denegada: No puedes realizar el cierre administrativo si las actividades en terreno no han finalizado.");

            // --- NUEVOS: Candados Financieros y Administrativos ---
            
            // 1. No se puede facturar sin Orden de Compra (Paso 6 y 15)
            if (input.FacturacionCompletada && string.IsNullOrWhiteSpace(input.NumeroOC))
                throw new GraphQLException("Bloqueo de Facturación: No puedes completar la facturación si el proyecto no tiene un Número de Orden de Compra (OC) registrado.");

            // 2. No se puede completar facturación si el valor facturado es 0 (Paso 15)
            if (input.FacturacionCompletada && input.ValorFacturado <= 0)
                throw new GraphQLException("Bloqueo Financiero: Has marcado la facturación como completada, pero el 'Total Facturado' es 0. Ingresa el valor real de la factura.");

            // 3. No se puede completar la liquidación sin registrar el gasto (Paso 12 y 14)
            if (input.LiquidacionTerminada && input.ValorGasto <= 0)
                throw new GraphQLException("Bloqueo Financiero: No puedes dar por terminada la liquidación del contratista si el 'Gasto Interno' es 0. Registra los costos primero.");

            // 4. No se puede finalizar sin Dossier (Paso 13 administrativo)
            if (proyecto.Estado == "Finalizado" && !input.DossierEntregado)
                 throw new GraphQLException("Falta Documentación: El Dossier debe estar entregado para poder finalizar el proyecto completamente.");
            // ==========================================
            // 4. MÁQUINA DE ESTADOS AUTOMÁTICA
            // ==========================================
            if (!string.IsNullOrEmpty(input.EstadoForzado) && 
               (input.EstadoForzado == "Cancelado" || input.EstadoForzado == "Incumplimiento"))
            {
                proyecto.Estado = input.EstadoForzado;
            }
            else if (proyecto.FechaFinalizacionActividades.HasValue) 
            {
                if (proyecto.DossierEntregado && proyecto.LiquidacionTerminada && proyecto.FacturacionCompletada) {
                    proyecto.Estado = "Finalizado"; 
                } else {
                    proyecto.Estado = "Pendiente cierre administrativo"; 
                }
            }
            else if (proyecto.FechaInicioActividades.HasValue)
            {
                proyecto.Estado = "En ejecución"; 
            }
            else if (proyecto.FechaAprobacionPresupuesto.HasValue || proyecto.FechaEnvioPresupuesto.HasValue || proyecto.FechaSolicitudPermisos.HasValue)
            {
                proyecto.Estado = "Preliminares"; 
            }
            else
            {
                proyecto.Estado = "Pendiente"; 
            }

            // 5. Candado de Calidad (Auditoría final antes de permitir el estado "Verde")
            if (input.DossierEntregado && input.LiquidacionTerminada && input.FacturacionCompletada)
            {
                if (!input.FechaRespuestaCliente.HasValue)
                    throw new GraphQLException("Auditoría fallida: Es obligatorio registrar la 'Respuesta a Cliente (SLA)' antes de cerrar el proyecto.");
                
                if (!input.FechaAprobacionPermisos.HasValue || !input.FechaSolicitudPermisos.HasValue)
                    throw new GraphQLException("Auditoría fallida: Faltan fechas en la sección de Permisos. Si el proyecto no requería permisos, debes ingresar la misma fecha de la respuesta al cliente, tal como indica el manual.");

                if (string.IsNullOrWhiteSpace(input.Polizas))
                    throw new GraphQLException("Auditoría fallida: Debes definir el Estado de Pólizas (Requiere, No requiere, etc.) antes de cerrar.");

                if (string.IsNullOrWhiteSpace(input.CentroDeCostos))
                    throw new GraphQLException("Auditoría fallida: El Centro de Costos es obligatorio para el cierre contable del proyecto.");
            }

            await context.SaveChangesAsync();
            return proyecto;
        }

        [Authorize(Policy = "proyectos.editar")]
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