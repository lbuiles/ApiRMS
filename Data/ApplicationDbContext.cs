using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Models;
using RmsErp.Api.Models.Clientes;
using RmsErp.Api.Models.Catalogos;
using RmsErp.Api.Models.Modulos;
using RmsErp.Api.Models.Usuarios;
using RmsErp.Api.Models.Tracker;
using RmsErp.Api.Models.Contratistas;

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
        public DbSet<Proyecto> Proyectos { get; set; }
        public DbSet<ProyectoObservacion> ProyectoObservaciones { get; set; }
        
        public DbSet<CategoriaProyecto> ProyectosCategorias { get; set; }

        // --- NUEVAS TABLAS DEL TRACKER ---
        public DbSet<ProyectoRequisicion> ProyectoRequisiciones { get; set; }
        public DbSet<ProyectoOrdenTrabajo> ProyectoOrdenesTrabajo { get; set; }
        public DbSet<ProyectoAnticipo> ProyectoAnticipos { get; set; }
        public DbSet<ProyectoAnticipoDirecto> ProyectoAnticiposDirectos { get; set; }

        // --- MÓDULO CONTRATISTAS Y PROVEEDORES ---
        public DbSet<ContratistaProveedor> ContratistaProveedores { get; set; }
        public DbSet<CPAccionista> CPAccionistas { get; set; }
        public DbSet<CPCuentaBancaria> CPCuentasBancarias { get; set; }
        public DbSet<CPReferencia> CPReferencias { get; set; }
        public DbSet<CPDocumento> CPDocumentos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Configuraciones de CategoriaProyecto ---
            modelBuilder.Entity<CategoriaProyecto>()
                .Property(c => c.PresupuestoTotal)
                .HasPrecision(18, 2);

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

            modelBuilder.Entity<ClientePoliza>()
                .HasKey(cp => new { cp.ClienteId, cp.TipoPolizaId });

            modelBuilder.Entity<ClienteServicio>()
                .HasKey(cs => new { cs.ClienteId, cs.TipoServicioId });

            modelBuilder.Entity<ClienteRegion>()
                .HasKey(cr => new { cr.ClienteId, cr.RegionId });

            // ==========================================
            // NUEVAS RELACIONES DEL TRACKER (Sub-flujos)
            // ==========================================

            // Requisiciones -> Proyectos
            modelBuilder.Entity<ProyectoRequisicion>()
                .HasOne(pr => pr.Proyecto)
                .WithMany(p => p.Requisiciones)
                .HasForeignKey(pr => pr.ProyectoId)
                .OnDelete(DeleteBehavior.Cascade);

            // OTs -> Proyectos
            modelBuilder.Entity<ProyectoOrdenTrabajo>()
                .HasOne(ot => ot.Proyecto)
                .WithMany(p => p.OrdenesTrabajo)
                .HasForeignKey(ot => ot.ProyectoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Anticipos -> OTs
            modelBuilder.Entity<ProyectoAnticipo>()
                .HasOne(a => a.OrdenTrabajo)
                .WithMany(ot => ot.Anticipos)
                .HasForeignKey(a => a.OrdenTrabajoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Anticipos -> Proyectos (Evitar ciclos en SQL Server)
            modelBuilder.Entity<ProyectoAnticipo>()
                .HasOne(a => a.Proyecto)
                .WithMany(p => p.Anticipos)
                .HasForeignKey(a => a.ProyectoId)
                .OnDelete(DeleteBehavior.NoAction); 
        }
    }
}