using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RmsErp.Api.Migrations
{
    /// <inheritdoc />
    public partial class TransformacionSistemaPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Roles_RolId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_RolId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "RolId",
                table: "Usuarios");

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioPermisos",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermisoId = table.Column<int>(type: "int", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioPermisos", x => new { x.UsuarioId, x.PermisoId });
                    table.ForeignKey(
                        name: "FK_UsuarioPermisos_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "Permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioPermisos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Area", "Nombre", "Slug" },
                values: new object[,]
                {
                    { 1, "Administracion", "Ver Usuarios", "admin.usuarios.leer" },
                    { 2, "Administracion", "Crear Usuarios", "admin.usuarios.crear" },
                    { 3, "Administracion", "Editar Usuarios", "admin.usuarios.editar" },
                    { 4, "Administracion", "Eliminar Usuarios", "admin.usuarios.borrar" },
                    { 5, "Administracion", "Ver Clientes", "admin.clientes.leer" },
                    { 6, "Administracion", "Crear Clientes", "admin.clientes.crear" },
                    { 7, "Administracion", "Editar Clientes", "admin.clientes.editar" },
                    { 8, "Administracion", "Eliminar Clientes", "admin.clientes.borrar" },
                    { 9, "Administracion", "Ver Contratistas", "admin.contratistas.leer" },
                    { 10, "Administracion", "Crear Contratistas", "admin.contratistas.crear" },
                    { 11, "Administracion", "Editar Contratistas", "admin.contratistas.editar" },
                    { 12, "Administracion", "Eliminar Contratistas", "admin.contratistas.borrar" },
                    { 13, "Proyectos", "Ver Energía", "proyectos.energia.leer" },
                    { 14, "Proyectos", "Crear Energía", "proyectos.energia.crear" },
                    { 15, "Proyectos", "Editar Energía", "proyectos.energia.editar" },
                    { 16, "Proyectos", "Eliminar Energía", "proyectos.energia.borrar" },
                    { 17, "Proyectos", "Ver Obra Civil", "proyectos.obracivil.leer" },
                    { 18, "Proyectos", "Crear Obra Civil", "proyectos.obracivil.crear" },
                    { 19, "Proyectos", "Editar Obra Civil", "proyectos.obracivil.editar" },
                    { 20, "Proyectos", "Eliminar Obra Civil", "proyectos.obracivil.borrar" },
                    { 21, "Proyectos", "Ver Telecomunicaciones", "proyectos.telecom.leer" },
                    { 22, "Proyectos", "Crear Telecomunicaciones", "proyectos.telecom.crear" },
                    { 23, "Proyectos", "Editar Telecomunicaciones", "proyectos.telecom.editar" },
                    { 24, "Proyectos", "Eliminar Telecomunicaciones", "proyectos.telecom.borrar" },
                    { 25, "Finanzas", "Ver Finanzas", "finanzas.leer" },
                    { 26, "Finanzas", "Crear Finanzas", "finanzas.crear" },
                    { 27, "Finanzas", "Editar Finanzas", "finanzas.editar" },
                    { 28, "Finanzas", "Eliminar Finanzas", "finanzas.borrar" },
                    { 29, "Gestion Humana", "Ver Gestión Humana", "gestionhumana.leer" },
                    { 30, "Gestion Humana", "Crear Gestión Humana", "gestionhumana.crear" },
                    { 31, "Gestion Humana", "Editar Gestión Humana", "gestionhumana.editar" },
                    { 32, "Gestion Humana", "Eliminar Gestión Humana", "gestionhumana.borrar" },
                    { 33, "SST", "Ver SST", "sst.leer" },
                    { 34, "SST", "Crear SST", "sst.crear" },
                    { 35, "SST", "Editar SST", "sst.editar" },
                    { 36, "SST", "Eliminar SST", "sst.borrar" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Slug",
                table: "Permisos",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioPermisos_PermisoId",
                table: "UsuarioPermisos",
                column: "PermisoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsuarioPermisos");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.AddColumn<int>(
                name: "RolId",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Descripcion", "Nombre" },
                values: new object[,]
                {
                    { 1, "Acceso total a RmsApp", "Admin" },
                    { 2, "Puede crear y editar, pero no eliminar", "Operador" },
                    { 3, "Solo lectura de información", "Consulta" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_RolId",
                table: "Usuarios",
                column: "RolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Roles_RolId",
                table: "Usuarios",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
