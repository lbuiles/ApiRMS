using HotChocolate;
using RmsErp.Api.Data;
using RmsErp.Api.Models;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RmsErp.Api.Models.Clientes;
using RmsErp.Api.Models.Catalogos;

namespace RmsErp.Api.Mutations.Clientes
{
    // --- INPUTS PARA LA ESTRUCTURA JERÁRQUICA ---
    
    public record ContactoInput(
        string Nombre,
        string Cargo,
        string Email,
        string Telefono
    );

    public record SucursalInput(
        string Nombre,
        string Departamento,
        string Ciudad,
        string Direccion,
        List<ContactoInput>? Contactos
    );

    public record ClienteInput(
        string RazonSocial,
        string Nit,
        string Departamento,
        string Ciudad,
        string Direccion,
        string Telefono,
        string Email,
        string ContactoPrincipal,
        string UsuarioCreacion,

        // ==========================================
        // NUEVOS CAMPOS (Catálogos y Configuraciones)
        // ==========================================
        int? TipoClienteId,
        
        // Condiciones Financieras
        int? TipoContratoId,
        int? CondicionPagoId,
        int? MonedaId,
        int? TipoFacturacionId,
        string? EmailFacturacion,
        bool RequiereOC,
        bool RequierePolizas,
        
        // Información Operativa
        int? PrioridadId,
        int SlaDias,

        // Arrays de Ids para las relaciones Muchos a Muchos
        List<int>? RegionesIds,
        List<int>? ServiciosIds,
        List<int>? PolizasIds,

        List<SucursalInput>? Sucursales
    );

    [ExtendObjectType("Mutation")]
    [Authorize]
    public class ClienteMutation
    {
        [Authorize(Policy = "admin.clientes.crear")]
        public async Task<Cliente> AddCliente(
            ClienteInput input, 
            [Service] ApplicationDbContext context)
        {
            // 1. Instanciamos el Cliente principal con sus extensiones 1 a 1
            var nuevoCliente = new Cliente
            {
                RazonSocial = input.RazonSocial,
                Nit = input.Nit,
                Departamento = input.Departamento,
                Ciudad = input.Ciudad,
                Direccion = input.Direccion,
                Telefono = input.Telefono,
                Email = input.Email,
                ContactoPrincipal = input.ContactoPrincipal,
                UsuarioCreacion = input.UsuarioCreacion,
                Estado = "ACTIVO",
                FechaCreacion = DateTime.UtcNow,
                TipoClienteId = input.TipoClienteId,

                // Crear Extensión Financiera (1 a 1)
                Condiciones = new ClienteCondicion
                {
                    TipoContratoId = input.TipoContratoId,
                    CondicionPagoId = input.CondicionPagoId,
                    MonedaId = input.MonedaId,
                    TipoFacturacionId = input.TipoFacturacionId,
                    EmailFacturacion = input.EmailFacturacion ?? string.Empty,
                    RequiereOC = input.RequiereOC,
                    RequierePolizas = input.RequierePolizas
                },

                // Crear Extensión Operativa (1 a 1)
                Operacion = new ClienteOperacion
                {
                    PrioridadId = input.PrioridadId,
                    SlaDias = input.SlaDias
                }
            };

            // 2. Mapeo de Relaciones Muchos a Muchos
            if (input.PolizasIds != null)
            {
                foreach (var id in input.PolizasIds)
                    nuevoCliente.ClientePolizas.Add(new ClientePoliza { TipoPolizaId = id });
            }

            if (input.ServiciosIds != null)
            {
                foreach (var id in input.ServiciosIds)
                    nuevoCliente.ClienteServicios.Add(new ClienteServicio { TipoServicioId = id });
            }

            if (input.RegionesIds != null)
            {
                foreach (var id in input.RegionesIds)
                    nuevoCliente.ClienteRegiones.Add(new ClienteRegion { RegionId = id });
            }

            // 3. Mapeo recursivo de Sucursales y sus Contactos (Tu lógica original intacta)
            if (input.Sucursales != null && input.Sucursales.Any())
            {
                foreach (var sInput in input.Sucursales)
                {
                    var nuevaSucursal = new ClienteSucursal
                    {
                        Nombre = sInput.Nombre,
                        Departamento = sInput.Departamento,
                        Ciudad = sInput.Ciudad,
                        Direccion = sInput.Direccion,
                        Estado = "ACTIVO"
                    };

                    if (sInput.Contactos != null && sInput.Contactos.Any())
                    {
                        foreach (var cInput in sInput.Contactos)
                        {
                            nuevaSucursal.Contactos.Add(new ClienteContacto
                            {
                                Nombre = cInput.Nombre,
                                Cargo = cInput.Cargo,
                                Email = cInput.Email,
                                Telefono = cInput.Telefono
                            });
                        }
                    }

                    nuevoCliente.Sucursales.Add(nuevaSucursal);
                }
            }

            // 4. Guardado atómico (Transaccional)
            context.Clientes.Add(nuevoCliente);
            await context.SaveChangesAsync(); 
            
            return nuevoCliente; 
        }

        [Authorize(Policy = "admin.clientes.editar")]
        public async Task<Cliente?> UpdateCliente(
            Guid id, 
            ClienteInput input, 
            [Service] ApplicationDbContext context)
        {
            // 1. Cargar el cliente con TODOS sus hijos y extensiones
            var cliente = await context.Clientes
                .Include(c => c.Condiciones)
                .Include(c => c.Operacion)
                .Include(c => c.ClientePolizas)
                .Include(c => c.ClienteServicios)
                .Include(c => c.ClienteRegiones)
                .Include(c => c.Sucursales)
                    .ThenInclude(s => s.Contactos)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null) return null;

            // 2. Actualizar datos básicos del padre
            cliente.RazonSocial = input.RazonSocial;
            cliente.Nit = input.Nit;
            cliente.Departamento = input.Departamento;
            cliente.Ciudad = input.Ciudad;
            cliente.Direccion = input.Direccion;
            cliente.Telefono = input.Telefono;
            cliente.Email = input.Email;
            cliente.ContactoPrincipal = input.ContactoPrincipal;
            cliente.UsuarioCreacion = input.UsuarioCreacion;
            cliente.TipoClienteId = input.TipoClienteId;

            // 3. Actualizar Extensiones 1 a 1
            if (cliente.Condiciones == null) cliente.Condiciones = new ClienteCondicion { ClienteId = id };
            cliente.Condiciones.TipoContratoId = input.TipoContratoId;
            cliente.Condiciones.CondicionPagoId = input.CondicionPagoId;
            cliente.Condiciones.MonedaId = input.MonedaId;
            cliente.Condiciones.TipoFacturacionId = input.TipoFacturacionId;
            cliente.Condiciones.EmailFacturacion = input.EmailFacturacion ?? string.Empty;
            cliente.Condiciones.RequiereOC = input.RequiereOC;
            cliente.Condiciones.RequierePolizas = input.RequierePolizas;

            if (cliente.Operacion == null) cliente.Operacion = new ClienteOperacion { ClienteId = id };
            cliente.Operacion.PrioridadId = input.PrioridadId;
            cliente.Operacion.SlaDias = input.SlaDias;

            // 4. ELIMINACIÓN Y RECREACIÓN DE RELACIONES N a N
            context.ClientesPolizas.RemoveRange(cliente.ClientePolizas);
            if (input.PolizasIds != null)
            {
                foreach (var polizaId in input.PolizasIds)
                    cliente.ClientePolizas.Add(new ClientePoliza { ClienteId = id, TipoPolizaId = polizaId });
            }

            context.ClientesServicios.RemoveRange(cliente.ClienteServicios);
            if (input.ServiciosIds != null)
            {
                foreach (var servicioId in input.ServiciosIds)
                    cliente.ClienteServicios.Add(new ClienteServicio { ClienteId = id, TipoServicioId = servicioId });
            }

            context.ClientesRegiones.RemoveRange(cliente.ClienteRegiones);
            if (input.RegionesIds != null)
            {
                foreach (var regionId in input.RegionesIds)
                    cliente.ClienteRegiones.Add(new ClienteRegion { ClienteId = id, RegionId = regionId });
            }

            // 5. ELIMINACIÓN Y RECREACIÓN DE SUCURSALES (Tu lógica intacta)
            foreach (var suc in cliente.Sucursales.ToList())
            {
                context.ClientesContactos.RemoveRange(suc.Contactos);
                context.ClientesSucursales.Remove(suc);
            }

            if (input.Sucursales != null)
            {
                foreach (var sInput in input.Sucursales)
                {
                    var nuevaSucursal = new ClienteSucursal
                    {
                        Id = Guid.NewGuid(), 
                        Nombre = sInput.Nombre,
                        Departamento = sInput.Departamento,
                        Ciudad = sInput.Ciudad,
                        Direccion = sInput.Direccion,
                        Estado = "ACTIVO",
                        ClienteId = id 
                    };

                    if (sInput.Contactos != null)
                    {
                        foreach (var cInput in sInput.Contactos)
                        {
                            nuevaSucursal.Contactos.Add(new ClienteContacto
                            {
                                Id = Guid.NewGuid(), 
                                Nombre = cInput.Nombre,
                                Cargo = cInput.Cargo,
                                Email = cInput.Email,
                                Telefono = cInput.Telefono
                            });
                        }
                    }
                    context.ClientesSucursales.Add(nuevaSucursal);
                }
            }

            try 
            {
                await context.SaveChangesAsync();
                return cliente;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.WriteLine("Error de concurrencia crítico: " + ex.Message);
                throw;
            }
        }

        [Authorize(Policy = "admin.clientes.borrar")]
        public async Task<bool> DeleteCliente(Guid id, [Service] ApplicationDbContext context)
        {
            var cliente = await context.Clientes.FindAsync(id);
            if (cliente == null) return false;

            cliente.Estado = "INACTIVO";
            await context.SaveChangesAsync();
            return true;
        }

        [Authorize(Policy = "admin.clientes.editar")]
        public async Task<bool> ActivateCliente(Guid id, [Service] ApplicationDbContext context)
        {
            var cliente = await context.Clientes.FindAsync(id);
            if (cliente == null) return false;

            cliente.Estado = "ACTIVO"; 
            await context.SaveChangesAsync();
            return true;
        }
    }
}