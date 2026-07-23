using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Tareas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RmsErp.Api.Mutations.Tareas
{
    // =========================================================
    // DTOs / INPUTS
    // =========================================================

    public record TareaInput(
        string Titulo,
        string? Descripcion,
        int EstadoId,
        DateTime SemanaProgramada,
        DateTime? FechaPresupuestoInicio,
        DateTime? FechaPresupuestoFin,
        Guid CreadoPorId,
        List<Guid>? UsuariosAsignados   // IDs de los responsables iniciales
    );

    public record TareaUpdateInput(
        string Titulo,
        string? Descripcion,
        DateTime? FechaPresupuestoInicio,
        DateTime? FechaPresupuestoFin,
        DateTime? FechaRealFinalizacion
    );

    public record MoverTareaInput(
        Guid TareaId,
        DateTime NuevaSemana,   // Cualquier día de la nueva semana; se normaliza a lunes
        Guid MovidoPorId,
        string? Motivo
    );

    public record CambiarEstadoInput(
        Guid TareaId,
        int NuevoEstadoId,
        Guid CambiadoPorId,
        DateTime? FechaRealFinalizacion   // El usuario elige la fecha real al completar
    );

    public record ComentarioInput(
        Guid TareaId,
        Guid UsuarioId,
        string Comentario
    );

    public record AsignarUsuarioInput(
        Guid TareaId,
        Guid UsuarioId,
        Guid AsignadoPorId
    );

    public record EstadoTareaInput(
        string Nombre,
        string Color,
        int Orden,
        bool EsEstadoInicial,
        bool EsEstadoFinal
    );

    // =========================================================
    // MUTATIONS PRINCIPALES
    // =========================================================

    [ExtendObjectType("Mutation")]
    [Authorize]
    public class TareaMutation
    {
        // ---------------------------------------------------------
        // CREAR TAREA
        // ---------------------------------------------------------
        [Authorize(Policy = "tareas.crear")]
        public async Task<Tarea> AddTarea(
            TareaInput input,
            [Service] ApplicationDbContext context)
        {
            // Normalizar al lunes de la semana
            var lunes = ObtenerLunesDeSemana(input.SemanaProgramada);

            // Validar que no sea una semana pasada
            var lunesActual = ObtenerLunesDeSemana(DateTime.UtcNow);
            if (lunes < lunesActual)
                throw new GraphQLException("No puedes programar tareas en semanas anteriores a la actual.");

            // Verificar que el estado existe
            var estado = await context.TareasEstados.FindAsync(input.EstadoId)
                ?? throw new GraphQLException("El estado seleccionado no existe.");

            var tarea = new Tarea
            {
                Titulo                 = input.Titulo,
                Descripcion            = input.Descripcion,
                EstadoId               = input.EstadoId,
                SemanaProgramada       = lunes,
                FechaPresupuestoInicio = input.FechaPresupuestoInicio,
                FechaPresupuestoFin    = input.FechaPresupuestoFin,
                CreadoPorId            = input.CreadoPorId,
                FechaCreacion          = DateTime.UtcNow,
                VecesMovida            = 0
            };

            context.Tareas.Add(tarea);

            // Registrar historial de estado inicial
            context.TareasHistorialEstados.Add(new TareaHistorialEstado
            {
                TareaId          = tarea.Id,
                EstadoAnteriorId = null,   // Creación: sin estado anterior
                EstadoNuevoId    = input.EstadoId,
                CambiadoPorId    = input.CreadoPorId,
                FechaCambio      = DateTime.UtcNow
            });

            // Asignar responsables iniciales
            if (input.UsuariosAsignados != null && input.UsuariosAsignados.Any())
            {
                foreach (var usuarioId in input.UsuariosAsignados.Distinct())
                {
                    context.TareasAsignados.Add(new TareaAsignado
                    {
                        TareaId         = tarea.Id,
                        UsuarioId       = usuarioId,
                        AsignadoPorId   = input.CreadoPorId,
                        FechaAsignacion = DateTime.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync();

            return await context.Tareas
                .Include(t => t.Estado)
                .Include(t => t.CreadoPor)
                .Include(t => t.Asignados).ThenInclude(a => a.Usuario)
                .FirstAsync(t => t.Id == tarea.Id);
        }

        // ---------------------------------------------------------
        // EDITAR TAREA (título, descripción, fechas)
        // ---------------------------------------------------------
        [Authorize(Policy = "tareas.crear")]
        public async Task<Tarea?> UpdateTarea(
            Guid id,
            TareaUpdateInput input,
            [Service] ApplicationDbContext context)
        {
            var tarea = await context.Tareas.FindAsync(id);
            if (tarea == null) return null;

            // Validación: si se registra fecha real, debe existir presupuesto de fin
            if (input.FechaRealFinalizacion.HasValue && !input.FechaPresupuestoFin.HasValue)
                throw new GraphQLException("Debe definir una fecha presupuestada de fin antes de registrar la fecha real.");

            tarea.Titulo                 = input.Titulo;
            tarea.Descripcion            = input.Descripcion;
            tarea.FechaPresupuestoInicio = input.FechaPresupuestoInicio;
            tarea.FechaPresupuestoFin    = input.FechaPresupuestoFin;
            tarea.FechaRealFinalizacion  = input.FechaRealFinalizacion;
            tarea.FechaActualizacion     = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return await context.Tareas
                .Include(t => t.Estado)
                .Include(t => t.CreadoPor)
                .Include(t => t.Asignados).ThenInclude(a => a.Usuario)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        // ---------------------------------------------------------
        // CAMBIAR ESTADO
        // ── Cualquier usuario con tareas.ver puede mover entre
        //    estados no finales. Solo tareas.finalizar puede
        //    pasar a un estado marcado como EsEstadoFinal.
        // ---------------------------------------------------------
        [Authorize(Policy = "tareas.ver")]
        public async Task<Tarea?> CambiarEstadoTarea(
            CambiarEstadoInput input,
            [Service] ApplicationDbContext context)
        {
            var tarea = await context.Tareas.FindAsync(input.TareaId);
            if (tarea == null) return null;

            var nuevoEstado = await context.TareasEstados.FindAsync(input.NuevoEstadoId)
                ?? throw new GraphQLException("El estado destino no existe.");

            // ── Validar permiso para estados finales ──────────────
            if (nuevoEstado.EsEstadoFinal)
            {
                var tieneFinalizar = await context.UsuarioPermisos
                    .AnyAsync(up => up.UsuarioId == input.CambiadoPorId
                                 && up.Permiso.Slug == "tareas.finalizar");

                if (!tieneFinalizar)
                    throw new GraphQLException(
                        ErrorBuilder.New()
                            .SetMessage("No tienes permiso para completar tareas. Se requiere 'tareas.finalizar'.")
                            .SetCode("FORBIDDEN")
                            .Build());
            }
            // ─────────────────────────────────────────────────────

            var estadoAnteriorId = tarea.EstadoId;

            // Si el nuevo estado es final, usar la fecha real proporcionada (o UtcNow como fallback)
            if (nuevoEstado.EsEstadoFinal && !tarea.FechaRealFinalizacion.HasValue)
                tarea.FechaRealFinalizacion = input.FechaRealFinalizacion?.ToUniversalTime() ?? DateTime.UtcNow;

            // Si se regresa de un estado final, limpiar fecha real
            var estadoAnterior = await context.TareasEstados.FindAsync(estadoAnteriorId);
            if (estadoAnterior?.EsEstadoFinal == true && !nuevoEstado.EsEstadoFinal)
                tarea.FechaRealFinalizacion = null;

            tarea.EstadoId           = input.NuevoEstadoId;
            tarea.FechaActualizacion = DateTime.UtcNow;

            // Registrar en historial de estados
            context.TareasHistorialEstados.Add(new TareaHistorialEstado
            {
                TareaId          = input.TareaId,
                EstadoAnteriorId = estadoAnteriorId,
                EstadoNuevoId    = input.NuevoEstadoId,
                CambiadoPorId    = input.CambiadoPorId,
                FechaCambio      = DateTime.UtcNow
            });

            await context.SaveChangesAsync();

            return await context.Tareas
                .Include(t => t.Estado)
                .Include(t => t.Asignados).ThenInclude(a => a.Usuario)
                .FirstOrDefaultAsync(t => t.Id == input.TareaId);
        }

        // ---------------------------------------------------------
        // MOVER A OTRA SEMANA
        // ---------------------------------------------------------
        [Authorize(Policy = "tareas.mover")]
        public async Task<Tarea?> MoverTareaSemana(
            MoverTareaInput input,
            [Service] ApplicationDbContext context)
        {
            var tarea = await context.Tareas.FindAsync(input.TareaId);
            if (tarea == null) return null;

            var semanaOrigen  = tarea.SemanaProgramada;
            var semanaDestino = ObtenerLunesDeSemana(input.NuevaSemana);

            if (semanaOrigen == semanaDestino)
                throw new GraphQLException("La tarea ya está en esa semana.");

            // Registrar el movimiento antes de actualizar
            context.TareasMovimientos.Add(new TareaMovimiento
            {
                TareaId         = input.TareaId,
                SemanaOrigen    = semanaOrigen,
                SemanaDestino   = semanaDestino,
                MovidoPorId     = input.MovidoPorId,
                FechaMovimiento = DateTime.UtcNow,
                Motivo          = input.Motivo
            });

            // Actualizar la tarea
            tarea.SemanaProgramada   = semanaDestino;
            tarea.VecesMovida       += 1;
            tarea.FechaActualizacion = DateTime.UtcNow;

            await context.SaveChangesAsync();

            return await context.Tareas
                .Include(t => t.Estado)
                .Include(t => t.Asignados).ThenInclude(a => a.Usuario)
                .Include(t => t.Movimientos.OrderByDescending(m => m.FechaMovimiento))
                .FirstOrDefaultAsync(t => t.Id == input.TareaId);
        }

        // ---------------------------------------------------------
        // AGREGAR COMENTARIO
        // ---------------------------------------------------------
        [Authorize(Policy = "tareas.ver")]
        public async Task<TareaComentario?> AddComentarioTarea(
            ComentarioInput input,
            [Service] ApplicationDbContext context)
        {
            var tarea = await context.Tareas.FindAsync(input.TareaId);
            if (tarea == null) return null;

            var comentario = new TareaComentario
            {
                TareaId       = input.TareaId,
                UsuarioId     = input.UsuarioId,
                Comentario    = input.Comentario,
                FechaRegistro = DateTime.UtcNow
            };

            context.TareasComentarios.Add(comentario);
            await context.SaveChangesAsync();

            return await context.TareasComentarios
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.Id == comentario.Id);
        }

        // ---------------------------------------------------------
        // ASIGNAR USUARIO A TAREA
        // ---------------------------------------------------------
        [Authorize(Policy = "tareas.asignar")]
        public async Task<Tarea?> AsignarUsuarioTarea(
            AsignarUsuarioInput input,
            [Service] ApplicationDbContext context)
        {
            var tarea = await context.Tareas.FindAsync(input.TareaId);
            if (tarea == null) return null;

            // Evitar duplicados
            var yaAsignado = await context.TareasAsignados
                .AnyAsync(a => a.TareaId == input.TareaId && a.UsuarioId == input.UsuarioId);

            if (!yaAsignado)
            {
                context.TareasAsignados.Add(new TareaAsignado
                {
                    TareaId         = input.TareaId,
                    UsuarioId       = input.UsuarioId,
                    AsignadoPorId   = input.AsignadoPorId,
                    FechaAsignacion = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            return await context.Tareas
                .Include(t => t.Estado)
                .Include(t => t.Asignados).ThenInclude(a => a.Usuario)
                .FirstOrDefaultAsync(t => t.Id == input.TareaId);
        }

        // ---------------------------------------------------------
        // DESASIGNAR USUARIO DE TAREA
        // ---------------------------------------------------------
        [Authorize(Policy = "tareas.asignar")]
        public async Task<bool> DesasignarUsuarioTarea(
            Guid tareaId,
            Guid usuarioId,
            [Service] ApplicationDbContext context)
        {
            var asignado = await context.TareasAsignados
                .FirstOrDefaultAsync(a => a.TareaId == tareaId && a.UsuarioId == usuarioId);

            if (asignado == null) return false;

            context.TareasAsignados.Remove(asignado);
            await context.SaveChangesAsync();
            return true;
        }

        // ---------------------------------------------------------
        // ELIMINAR TAREA
        // ── Requiere tareas.mover (permiso de supervisor),
        //    ya que es una acción privilegiada igual que mover.
        // ---------------------------------------------------------
        [Authorize(Policy = "tareas.mover")]
        public async Task<bool> DeleteTarea(
            Guid id,
            [Service] ApplicationDbContext context)
        {
            var tarea = await context.Tareas.FindAsync(id);
            if (tarea == null) return false;

            context.Tareas.Remove(tarea);
            await context.SaveChangesAsync();
            return true;
        }

        // =========================================================
        // ADMINISTRACIÓN DE ESTADOS (solo tareas.estados.admin)
        // =========================================================

        [Authorize(Policy = "tareas.estados.admin")]
        public async Task<EstadoTarea> AddEstadoTarea(
            EstadoTareaInput input,
            [Service] ApplicationDbContext context)
        {
            // Solo puede haber un estado inicial y un estado final
            if (input.EsEstadoInicial)
            {
                var existeInicial = await context.TareasEstados.AnyAsync(e => e.EsEstadoInicial && e.Activo);
                if (existeInicial)
                    throw new GraphQLException("Ya existe un estado marcado como inicial. Desactívalo primero.");
            }
            if (input.EsEstadoFinal)
            {
                var existeFinal = await context.TareasEstados.AnyAsync(e => e.EsEstadoFinal && e.Activo);
                if (existeFinal)
                    throw new GraphQLException("Ya existe un estado marcado como final. Desactívalo primero.");
            }

            var estado = new EstadoTarea
            {
                Nombre          = input.Nombre,
                Color           = input.Color,
                Orden           = input.Orden,
                EsEstadoInicial = input.EsEstadoInicial,
                EsEstadoFinal   = input.EsEstadoFinal,
                Activo          = true
            };

            context.TareasEstados.Add(estado);
            await context.SaveChangesAsync();
            return estado;
        }

        [Authorize(Policy = "tareas.estados.admin")]
        public async Task<EstadoTarea?> UpdateEstadoTarea(
            int id,
            EstadoTareaInput input,
            [Service] ApplicationDbContext context)
        {
            var estado = await context.TareasEstados.FindAsync(id);
            if (estado == null) return null;

            estado.Nombre          = input.Nombre;
            estado.Color           = input.Color;
            estado.Orden           = input.Orden;
            estado.EsEstadoInicial = input.EsEstadoInicial;
            estado.EsEstadoFinal   = input.EsEstadoFinal;

            await context.SaveChangesAsync();
            return estado;
        }

        [Authorize(Policy = "tareas.estados.admin")]
        public async Task<bool> ToggleEstadoTarea(
            int id,
            [Service] ApplicationDbContext context)
        {
            var estado = await context.TareasEstados.FindAsync(id);
            if (estado == null) return false;

            // No se puede desactivar si hay tareas activas en ese estado
            if (estado.Activo)
            {
                var tieneTareas = await context.Tareas.AnyAsync(t => t.EstadoId == id);
                if (tieneTareas)
                    throw new GraphQLException($"No puedes desactivar el estado '{estado.Nombre}' porque tiene tareas activas asignadas.");
            }

            estado.Activo = !estado.Activo;
            await context.SaveChangesAsync();
            return true;
        }

        // =========================================================
        // HELPER
        // =========================================================
        private static DateTime ObtenerLunesDeSemana(DateTime fecha)
        {
            var diaSemana  = (int)fecha.DayOfWeek;
            var diasARestar = diaSemana == 0 ? 6 : diaSemana - 1;
            return fecha.Date.AddDays(-diasARestar);
        }
    }
}