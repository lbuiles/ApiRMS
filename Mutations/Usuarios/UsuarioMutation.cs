using HotChocolate;
using RmsErp.Api.Data;
using RmsErp.Api.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Mutations.Usuarios
{
    [GraphQLName("UserInputDto")]
    public record UsuarioInput(string Nombre, string Email);

    [ExtendObjectType("Mutation")]
    [Authorize]
    public class UsuarioMutation
    {
        [Authorize(Policy = "admin.usuarios.crear")]
        public async Task<Usuario> AddUsuario(
            UsuarioInput input, 
            List<string> permisos, 
            [Service] ApplicationDbContext context)
        {
            var nuevoUsuario = new Usuario
            {
                Id = Guid.NewGuid(),
                Nombre = input.Nombre,
                Email = input.Email,
                Estado = "ACTIVO",
                FechaRegistro = DateTime.UtcNow
            };

            context.Usuarios.Add(nuevoUsuario);

            if (permisos != null && permisos.Any())
            {
                var permisosDb = await context.Permisos
                    .Where(p => permisos.Contains(p.Slug))
                    .ToListAsync();

                foreach (var permiso in permisosDb)
                {
                    context.UsuarioPermisos.Add(new UsuarioPermiso
                    {
                        UsuarioId = nuevoUsuario.Id,
                        PermisoId = permiso.Id,
                        FechaAsignacion = DateTime.Now 
                    });
                }
            }

            await context.SaveChangesAsync();
            
            return await context.Usuarios
                .Include(u => u.UsuarioPermisos)
                .ThenInclude(up => up.Permiso)
                .FirstAsync(u => u.Id == nuevoUsuario.Id);
        }

        [Authorize(Policy = "admin.usuarios.editar")]
        public async Task<Usuario?> UpdateUsuario(
            Guid id, 
            UsuarioInput input, 
            List<string> permisos, 
            [Service] ApplicationDbContext context)
        {
            var usuario = await context.Usuarios
                .Include(u => u.UsuarioPermisos)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null) return null;

            usuario.Nombre = input.Nombre;
            usuario.Email = input.Email;

            context.UsuarioPermisos.RemoveRange(usuario.UsuarioPermisos);

            if (permisos != null && permisos.Any())
            {
                var permisosDb = await context.Permisos
                    .Where(p => permisos.Contains(p.Slug))
                    .ToListAsync();

                foreach (var permiso in permisosDb)
                {
                    context.UsuarioPermisos.Add(new UsuarioPermiso
                    {
                        UsuarioId = id,
                        PermisoId = permiso.Id,
                        FechaAsignacion = DateTime.Now
                    });
                }
            }

            await context.SaveChangesAsync();

            return await context.Usuarios
                .Include(u => u.UsuarioPermisos)
                .ThenInclude(up => up.Permiso)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        [Authorize(Policy = "admin.usuarios.borrar")]
        public async Task<bool> DeleteUsuario(Guid id, [Service] ApplicationDbContext context)
        {
            var usuario = await context.Usuarios.FindAsync(id);
            if (usuario == null) return false;

            usuario.Estado = "INACTIVO";
            await context.SaveChangesAsync();
            return true;
        }

        [Authorize(Policy = "admin.usuarios.borrar")]
        public async Task<bool> ActivateUsuario(Guid id, [Service] ApplicationDbContext context)
        {
            var usuario = await context.Usuarios.FindAsync(id);
            if (usuario == null) return false;

            usuario.Estado = "ACTIVO";
            await context.SaveChangesAsync();
            return true;
        }
    }
}