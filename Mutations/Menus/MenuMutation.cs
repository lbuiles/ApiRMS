using HotChocolate;
using HotChocolate.Authorization;
using RmsErp.Api.Data;
using RmsErp.Api.Models;
using RmsErp.Api.Models.Modulos;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Mutations.Menus;

[GraphQLName("ModuloInputDto")]
public record ModuloInput(string Nombre, string Ruta, string Icono, int Orden, string SlugRaiz, string Area);

[GraphQLName("PermisoInputDto")]
public record PermisoInput(string Nombre, string Slug, int ModuloId);

[ExtendObjectType("Mutation")]
public class MenuMutation
{
    [Authorize(Policy = "admin.configuracion.editar")]
    public async Task<Modulo> CreateModulo(
        [Service] ApplicationDbContext context, 
        [GraphQLName("moduloInputData")] ModuloInput input)
    {
        var nuevoModulo = new Modulo
        {
            Nombre = input.Nombre,
            Ruta = input.Ruta,
            Icono = input.Icono,
            Orden = input.Orden,
            SlugRaiz = input.SlugRaiz
        };

        context.Modulos.Add(nuevoModulo);
        await context.SaveChangesAsync();
        return nuevoModulo;
    }

    [Authorize(Policy = "admin.configuracion.editar")]
    public async Task<Permiso> CreatePermiso(
        [Service] ApplicationDbContext context, 
        [GraphQLName("permisoInputData")] PermisoInput input)
    {
        var nuevoPermiso = new Permiso
        {
            Nombre = input.Nombre,
            Slug = input.Slug,
            ModuloId = input.ModuloId
        };

        context.Permisos.Add(nuevoPermiso);
        await context.SaveChangesAsync();
        return nuevoPermiso;
    }
}