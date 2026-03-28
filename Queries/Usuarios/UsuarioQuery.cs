using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using RmsErp.Api.Models;
using RmsErp.Api.Models.Usuarios;
using System.Security.Claims;

namespace RmsErp.Api.Queries.Usuarios;

[ExtendObjectType("Query")]
public class UsuarioQuery
{
    [Authorize(Policy = "admin.usuarios.leer")]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Usuario> GetUsuarios([Service] ApplicationDbContext context)
    {
        return context.Usuarios
            .Include(u => u.UsuarioPermisos)
                .ThenInclude(up => up.Permiso)
                    .ThenInclude(p => p.ModuloRelacion); 
    }

    [Authorize]
    public async Task<Usuario?> GetMiPerfil(
        [Service] ApplicationDbContext context, 
        ClaimsPrincipal principal)
    {
        var email = principal.FindFirst(ClaimTypes.Email)?.Value 
                    ?? principal.FindFirst("email")?.Value
                    ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
        
        if (string.IsNullOrEmpty(email)) return null;

        return await context.Usuarios
            .Include(u => u.UsuarioPermisos)
                .ThenInclude(up => up.Permiso)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    [Authorize(Policy = "admin.permisos.leer")]
    public async Task<List<Permiso>> GetTodosLosPermisos([Service] ApplicationDbContext context)
    {
        return await context.Permisos
            .Include(p => p.ModuloRelacion)
            .Where(p => p.ModuloId != null)
            .OrderBy(p => p.ModuloRelacion!.Area) 
            .ThenBy(p => p.ModuloRelacion!.Nombre)
            .ToListAsync();
    }
}