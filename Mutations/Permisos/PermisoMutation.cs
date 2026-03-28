using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Data;
using RmsErp.Api.Models;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Mutations.Permisos
{
    [ExtendObjectType("Mutation")]
    [Authorize]
    public class PermisoMutation
    {
        [Authorize(Policy = "admin.configuracion.editar")]
        public async Task<Permiso?> UpdatePermiso(
            int id, 
            string nombre, 
            int moduloId, 
            [Service] ApplicationDbContext context)
        {
            var permiso = await context.Permisos.FindAsync(id);
            if (permiso == null) return null;

            var moduloExiste = await context.Modulos.AnyAsync(m => m.Id == moduloId);
            if (!moduloExiste)
            {
                throw new GraphQLException("El Módulo especificado no existe.");
            }

            permiso.Nombre = nombre;
            permiso.ModuloId = moduloId;

            await context.SaveChangesAsync();

            return await context.Permisos
                .Include(p => p.ModuloRelacion)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        [Authorize(Policy = "admin.configuracion.eliminar")]
        public async Task<bool> DeletePermiso(int id, [Service] ApplicationDbContext context)
        {
            var permiso = await context.Permisos.FindAsync(id);
            if (permiso == null) throw new GraphQLException("El permiso no existe en la base de datos.");

            var usuariosAsociados = await context.UsuarioPermisos.AnyAsync(up => up.PermisoId == id);
            if (usuariosAsociados)
            {
                throw new GraphQLException("Bloqueo de Seguridad: No puedes eliminar este permiso porque hay usuarios que lo tienen asignado.");
            }

            context.Permisos.Remove(permiso);
            await context.SaveChangesAsync();
            return true;
        }
    }
}