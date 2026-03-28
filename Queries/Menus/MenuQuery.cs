using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using RmsErp.Api.Models;
using RmsErp.Api.Models.Modulos;

namespace RmsErp.Api.Queries.Menus;

[ExtendObjectType("Query")]
public class MenuQuery
{
    [Authorize]
    public async Task<List<Modulo>> GetMenuConfig(
        [Service] ApplicationDbContext context)
    {

        return await context.Modulos
            .Include(m => m.ListaPermisos) 
            .OrderBy(m => m.Orden)
            .ToListAsync();
    }
}