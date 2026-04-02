using HotChocolate;
using HotChocolate.Data;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Tracker;
using HotChocolate.Authorization;
using System.Linq;
using RmsErp.Api.Models.Clientes; // <-- Asegúrate de importar tus modelos de Cliente
using RmsErp.Api.Models.Usuarios;
using Microsoft.EntityFrameworkCore; // <-- Asegúrate de importar tus modelos de Usuario

namespace RmsErp.Api.Queries.Tracker
{
    [ExtendObjectType("Query")]
    [Authorize]
    public class ProyectoQuery
    {
        [Authorize(Policy = "proyectos.leer")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Proyecto> GetProyectos([Service] ApplicationDbContext context)
        {
            return context.Proyectos;
        }

        [Authorize(Policy = "proyectos.leer")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ProyectoObservacion> GetObservacionesProyecto([Service] ApplicationDbContext context)
        {
            return context.ProyectoObservaciones.OrderByDescending(o => o.FechaRegistro);
        }

        // =========================================================
        // NUEVAS CONSULTAS PARA LOS COMBOS DEL FORMULARIO
        // =========================================================

        [Authorize(Policy = "proyectos.leer")] // Solo requiere ver proyectos
        [UseSorting]
        public IQueryable<Cliente> GetClientesTracker([Service] ApplicationDbContext context)
        {
            // Solo trae los datos necesarios para el combo, sin exponer información sensible
            return context.Clientes.OrderBy(c => c.RazonSocial);
        }

        [Authorize(Policy = "proyectos.leer")]
        [UseSorting]
        public IQueryable<Usuario> GetUsuariosTracker([Service] ApplicationDbContext context)
        {
            // Solo trae usuarios activos
            return context.Usuarios.Where(u => u.Estado == "ACTIVO").OrderBy(u => u.Nombre);
        }

        [Authorize(Policy = "proyectos.leer")]
        public async Task<List<CategoriaProyecto>> GetCategoriasProyecto(
            [Service] ApplicationDbContext context)
        {
            // 1. Traemos las 3 categorías configuradas en la base de datos
            var categorias = await context.ProyectosCategorias.ToListAsync();

            // 2. Calculamos los totales dinámicamente
            foreach (var cat in categorias)
            {
                // Filtramos los proyectos que pertenecen a esta línea de negocio (Id de la categoría)
                var proyectosDeEstaLinea = context.Proyectos
                    .Where(p => p.LineaNegocio == cat.Id);

                // Contamos cuántos no están finalizados o cancelados (ajusta según tus estados)
                cat.CantidadActivos = await proyectosDeEstaLinea
                    .CountAsync(p => p.Estado != "Finalizado" && p.Estado != "Cancelado");

                // Sumamos el valor de las Órdenes de Compra (ValorOC)
                cat.PresupuestoTotal = await proyectosDeEstaLinea
                    .SumAsync(p => p.ValorOC);
            }

            return categorias;
        }
    }
}