using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Models;
using RmsErp.Api.Models.Clientes;
using RmsErp.Api.Models.Catalogos;
using RmsErp.Api.Models.Modulos;
using RmsErp.Api.Models.Usuarios;

namespace RmsErp.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Permiso> Permisos { get; set; }
        public DbSet<Modulo> Modulos { get; set; }
        public DbSet<UsuarioPermiso> UsuarioPermisos { get; set; }
        public DbSet<ClienteSucursal> ClientesSucursales { get; set; }
        public DbSet<ClienteContacto> ClientesContactos { get; set; }
        public DbSet<TipoCliente> TiposCliente { get; set; }
        public DbSet<TipoContrato> TiposContrato { get; set; }
        public DbSet<Moneda> Monedas { get; set; }
        public DbSet<TipoFacturacion> TiposFacturacion { get; set; }
        public DbSet<Prioridad> Prioridades { get; set; }
        public DbSet<CondicionPago> CondicionesPago { get; set; }
        public DbSet<TipoPoliza> TiposPoliza { get; set; }
        public DbSet<TipoServicio> TiposServicio { get; set; }
        public DbSet<Region> Regiones { get; set; }
        public DbSet<ClienteCondicion> ClientesCondiciones { get; set; }
        public DbSet<ClienteOperacion> ClientesOperaciones { get; set; }
        public DbSet<ClientePoliza> ClientesPolizas { get; set; }
        public DbSet<ClienteServicio> ClientesServicios { get; set; }
        public DbSet<ClienteRegion> ClientesRegiones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Configuraciones Originales ---
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<UsuarioPermiso>()
                .HasKey(up => new { up.UsuarioId, up.PermisoId });

            modelBuilder.Entity<Permiso>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            modelBuilder.Entity<Permiso>()
                .HasOne(p => p.ModuloRelacion)
                .WithMany(m => m.ListaPermisos)
                .HasForeignKey(p => p.ModuloId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ClienteSucursal>()
                .HasOne(s => s.Cliente)
                .WithMany(c => c.Sucursales)
                .HasForeignKey(s => s.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClienteContacto>()
                .HasOne(c => c.Sucursal)
                .WithMany(s => s.Contactos)
                .HasForeignKey(c => c.SucursalId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.Condiciones)
                .WithOne(cc => cc.Cliente)
                .HasForeignKey<ClienteCondicion>(cc => cc.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cliente>()
                .HasOne(c => c.Operacion)
                .WithOne(co => co.Cliente)
                .HasForeignKey<ClienteOperacion>(co => co.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);

            // B. Llaves Primarias Compuestas para las tablas puente (N a N)
            modelBuilder.Entity<ClientePoliza>()
                .HasKey(cp => new { cp.ClienteId, cp.TipoPolizaId });

            modelBuilder.Entity<ClienteServicio>()
                .HasKey(cs => new { cs.ClienteId, cs.TipoServicioId });

            modelBuilder.Entity<ClienteRegion>()
                .HasKey(cr => new { cr.ClienteId, cr.RegionId });
        }
    }
}