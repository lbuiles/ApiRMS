using Microsoft.EntityFrameworkCore;
using RmsErp.Api.Models;
using RmsErp.Api.Models.Clientes;
using RmsErp.Api.Models.Catalogos;
using RmsErp.Api.Models.Modulos;
using RmsErp.Api.Models.Usuarios;
using RmsErp.Api.Models.Tracker;
using RmsErp.Api.Models.Contratistas;
using RmsErp.Api.Models.Tareas;
using RmsErp.Api.Models.Cuadrillas;

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

        // --- MÓDULO TAREAS ---
        public DbSet<EstadoTarea> TareasEstados { get; set; }
        public DbSet<Tarea> Tareas { get; set; }
        public DbSet<TareaAsignado> TareasAsignados { get; set; }
        public DbSet<TareaComentario> TareasComentarios { get; set; }
        public DbSet<TareaMovimiento> TareasMovimientos { get; set; }
        public DbSet<TareaHistorialEstado> TareasHistorialEstados { get; set; }

        // --- MÓDULO NOTIFICACIONES ---
        public DbSet<Notificacion> Notificaciones { get; set; }

        // --- Módulo Cuadrillas (submenú de Proyectos) ---
        public DbSet<Cuadrilla> Cuadrillas { get; set; }
        public DbSet<CuadrillaMiembro> CuadrillasMiembros { get; set; }
        public DbSet<CuadrillaActividad> CuadrillasActividades { get; set; }
        public DbSet<CuadrillaActividadHistorial> CuadrillasActividadHistorial { get; set; }
        public DbSet<CuadrillaCompensatorio> CuadrillasCompensatorios { get; set; }
        public DbSet<CuadrillaConfigHoras> CuadrillasConfigHoras { get; set; }
        public DbSet<CuadrillaFestivo> CuadrillasFestivos { get; set; }
        public DbSet<CuadrillaParametro> CuadrillasParametros { get; set; }
        public DbSet<CuadrillaDiaImproductivo> CuadrillasDiasImproductivos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // MÓDULO CUADRILLAS
            // (las tablas se crean con script SQL manual: sql_cuadrillas_fase1.sql)
            // ==========================================
            modelBuilder.Entity<CuadrillaMiembro>(e =>
            {
                e.HasKey(m => new { m.CuadrillaId, m.UsuarioId });
                e.HasOne(m => m.Cuadrilla).WithMany(c => c.Miembros)
                 .HasForeignKey(m => m.CuadrillaId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(m => m.Usuario).WithMany()
                 .HasForeignKey(m => m.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CuadrillaActividad>(e =>
            {
                e.HasOne(a => a.Tecnico).WithMany().HasForeignKey(a => a.TecnicoId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.Cuadrilla).WithMany().HasForeignKey(a => a.CuadrillaId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.Proyecto).WithMany().HasForeignKey(a => a.ProyectoId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.SupervisorContacto).WithMany().HasForeignKey(a => a.SupervisorContactoId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.SupervisorUsuario).WithMany().HasForeignKey(a => a.SupervisorUsuarioId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.Creador).WithMany().HasForeignKey(a => a.CreadoPorId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.ActualizadoPor).WithMany().HasForeignKey(a => a.ActualizadoPorId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(a => a.Historial).WithOne().HasForeignKey(h => h.ActividadId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(a => new { a.TecnicoId, a.Fecha });
            });

            modelBuilder.Entity<CuadrillaActividadHistorial>()
                .HasOne(h => h.Usuario).WithMany().HasForeignKey(h => h.UsuarioId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CuadrillaCompensatorio>()
                .HasOne(x => x.Tecnico).WithMany().HasForeignKey(x => x.TecnicoId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CuadrillaConfigHoras>()
                .HasOne(x => x.Tecnico).WithMany().HasForeignKey(x => x.TecnicoId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CuadrillaDiaImproductivo>(e =>
            {
                e.HasOne(x => x.Tecnico).WithMany().HasForeignKey(x => x.TecnicoId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(x => new { x.TecnicoId, x.Fecha }).IsUnique();
            });

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

            // ==========================================
            // MÓDULO TAREAS
            // ==========================================

            // Clave compuesta de TareaAsignado
            modelBuilder.Entity<TareaAsignado>()
                .HasKey(ta => new { ta.TareaId, ta.UsuarioId });

            // Tarea → Estado (Restrict para no eliminar estado con tareas activas)
            modelBuilder.Entity<Tarea>()
                .HasOne(t => t.Estado)
                .WithMany(e => e.Tareas)
                .HasForeignKey(t => t.EstadoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tarea → CreadoPor (Restrict, evitar ciclo de cascadas con Usuarios)
            modelBuilder.Entity<Tarea>()
                .HasOne(t => t.CreadoPor)
                .WithMany()
                .HasForeignKey(t => t.CreadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            // TareaAsignado → Tarea (CASCADE: al eliminar tarea, se eliminan asignados)
            modelBuilder.Entity<TareaAsignado>()
                .HasOne(ta => ta.Tarea)
                .WithMany(t => t.Asignados)
                .HasForeignKey(ta => ta.TareaId)
                .OnDelete(DeleteBehavior.Cascade);

            // TareaAsignado → Usuario (Restrict, evitar ciclo de cascadas con Usuarios)
            modelBuilder.Entity<TareaAsignado>()
                .HasOne(ta => ta.Usuario)
                .WithMany()
                .HasForeignKey(ta => ta.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TareaAsignado>()
                .HasOne(ta => ta.AsignadoPor)
                .WithMany()
                .HasForeignKey(ta => ta.AsignadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            // TareaComentario → Tarea (CASCADE)
            modelBuilder.Entity<TareaComentario>()
                .HasOne(tc => tc.Tarea)
                .WithMany(t => t.Comentarios)
                .HasForeignKey(tc => tc.TareaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TareaComentario>()
                .HasOne(tc => tc.Usuario)
                .WithMany()
                .HasForeignKey(tc => tc.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            // TareaMovimiento → Tarea (CASCADE)
            modelBuilder.Entity<TareaMovimiento>()
                .HasOne(tm => tm.Tarea)
                .WithMany(t => t.Movimientos)
                .HasForeignKey(tm => tm.TareaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TareaMovimiento>()
                .HasOne(tm => tm.MovidoPor)
                .WithMany()
                .HasForeignKey(tm => tm.MovidoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            // TareaHistorialEstado → Tarea (CASCADE)
            modelBuilder.Entity<TareaHistorialEstado>()
                .HasOne(h => h.Tarea)
                .WithMany(t => t.HistorialEstados)
                .HasForeignKey(h => h.TareaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Historial → EstadoAnterior (Restrict, puede ser null en la creación inicial)
            modelBuilder.Entity<TareaHistorialEstado>()
                .HasOne(h => h.EstadoAnterior)
                .WithMany()
                .HasForeignKey(h => h.EstadoAnteriorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Historial → EstadoNuevo (Restrict)
            modelBuilder.Entity<TareaHistorialEstado>()
                .HasOne(h => h.EstadoNuevo)
                .WithMany()
                .HasForeignKey(h => h.EstadoNuevoId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TareaHistorialEstado>()
                .HasOne(h => h.CambiadoPor)
                .WithMany()
                .HasForeignKey(h => h.CambiadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // MÓDULO NOTIFICACIONES
            // ==========================================

            modelBuilder.Entity<Notificacion>(e =>
            {
                e.HasKey(n => n.Id);

                // Índice para consultas frecuentes por usuario y estado de lectura
                e.HasIndex(n => new { n.UsuarioDestinoId, n.Leida, n.FechaCreacion });

                // Notificacion → Usuario (CASCADE: al eliminar usuario, se eliminan sus notificaciones)
                e.HasOne(n => n.UsuarioDestino)
                 .WithMany()
                 .HasForeignKey(n => n.UsuarioDestinoId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}