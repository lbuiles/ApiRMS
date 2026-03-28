using HotChocolate;
using HotChocolate.Data;
using RmsErp.Api.Data;
using HotChocolate.Authorization;
using System.Linq;
using RmsErp.Api.Models.Clientes;
using RmsErp.Api.Models.Catalogos; // <-- IMPORTANTE: Agregar este using

namespace RmsErp.Api.Queries.Clientes
{
    [ExtendObjectType("Query")]
    [Authorize]
    public class ClienteQuery
    {
        [Authorize(Policy = "admin.clientes.leer")]
        [UseProjection] 
        [UseFiltering]
        [UseSorting]
        public IQueryable<Cliente> GetClientes([Service] ApplicationDbContext context)
        {
            return context.Clientes;
        }


        [Authorize(Policy = "admin.clientes.leer")]
        [UseSorting]
        public IQueryable<TipoCliente> GetTiposCliente([Service] ApplicationDbContext context) =>
            context.TiposCliente.Where(x => x.Activo);

        [Authorize(Policy = "admin.clientes.leer")]
        [UseSorting]
        public IQueryable<TipoContrato> GetTiposContrato([Service] ApplicationDbContext context) =>
            context.TiposContrato.Where(x => x.Activo);

        [Authorize(Policy = "admin.clientes.leer")]
        [UseSorting]
        public IQueryable<Moneda> GetMonedas([Service] ApplicationDbContext context) =>
            context.Monedas.Where(x => x.Activo);

        [Authorize(Policy = "admin.clientes.leer")]
        [UseSorting]
        public IQueryable<TipoFacturacion> GetTiposFacturacion([Service] ApplicationDbContext context) =>
            context.TiposFacturacion.Where(x => x.Activo);

        [Authorize(Policy = "admin.clientes.leer")]
        [UseSorting]
        public IQueryable<Prioridad> GetPrioridades([Service] ApplicationDbContext context) =>
            context.Prioridades.Where(x => x.Activo).OrderBy(x => x.Nivel); // Ordenados por nivel de criticidad

        [Authorize(Policy = "admin.clientes.leer")]
        [UseSorting]
        public IQueryable<CondicionPago> GetCondicionesPago([Service] ApplicationDbContext context) =>
            context.CondicionesPago.Where(x => x.Activo);

        [Authorize(Policy = "admin.clientes.leer")]
        [UseSorting]
        public IQueryable<TipoPoliza> GetTiposPoliza([Service] ApplicationDbContext context) =>
            context.TiposPoliza.Where(x => x.Activo);

        [Authorize(Policy = "admin.clientes.leer")]
        [UseSorting]
        public IQueryable<TipoServicio> GetTiposServicio([Service] ApplicationDbContext context) =>
            context.TiposServicio.Where(x => x.Activo);

        [Authorize(Policy = "admin.clientes.leer")]
        [UseSorting]
        public IQueryable<Region> GetRegiones([Service] ApplicationDbContext context) =>
            context.Regiones.Where(x => x.Activo);
    }
}