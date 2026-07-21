using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using RmsErp.Api.Data;
using RmsErp.Api.Models.Contratistas;
using System.Linq;

namespace RmsErp.Api.Queries.Contratistas
{
    [ExtendObjectType("Query")]
    public class ContratistaProveedorQuery
    {
        [Authorize(Policy = "admin.contratistas.leer")]
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<ContratistaProveedor> GetContratistaProveedores(
            [Service] ApplicationDbContext context)
            => context.ContratistaProveedores;
    }
}