using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RmsErp.Api.Migrations
{
    /// <inheritdoc />
    public partial class SistemaModulosDinamicos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Modulo",
                table: "Permisos");

            migrationBuilder.AddColumn<int>(
                name: "ModuloId",
                table: "Permisos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Modulos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ruta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    SlugRaiz = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modulos", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Modulos",
                columns: new[] { "Id", "Icono", "Nombre", "Orden", "Ruta", "SlugRaiz" },
                values: new object[,]
                {
                    { 1, "M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6", "Panel de Control", 1, "/panel", "panel" },
                    { 2, "M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z M15 12a3 3 0 11-6 0 3 3 0 016 0z", "Administración", 2, "/administracion", "admin" },
                    { 3, "M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10", "Proyectos", 3, "/proyectos", "proyectos" },
                    { 4, "M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z", "Finanzas", 4, "/finanzas", "finanzas" },
                    { 5, "M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z", "Gestión Humana", 5, "/gestion-humana", "gestionhumana" }
                });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 1,
                column: "ModuloId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 2,
                column: "ModuloId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 3,
                column: "ModuloId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 4,
                column: "ModuloId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 5,
                column: "ModuloId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 6,
                column: "ModuloId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 7,
                column: "ModuloId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 8,
                column: "ModuloId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 9,
                column: "ModuloId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 10,
                column: "ModuloId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 11,
                column: "ModuloId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 12,
                column: "ModuloId",
                value: 2);

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Administracion", 2, "Ver Catálogo de Permisos", "admin.permisos.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Administracion", 2, "Crear Catálogo de Permisos", "admin.permisos.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Administracion", 2, "Editar Catálogo de Permisos", "admin.permisos.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Administracion", 2, "Eliminar Catálogo de Permisos", "admin.permisos.borrar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "ModuloId", "Nombre", "Slug" },
                values: new object[] { 3, "Ver Energía", "proyectos.energia.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "ModuloId", "Nombre", "Slug" },
                values: new object[] { 3, "Crear Energía", "proyectos.energia.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "ModuloId", "Nombre", "Slug" },
                values: new object[] { 3, "Editar Energía", "proyectos.energia.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "ModuloId", "Nombre", "Slug" },
                values: new object[] { 3, "Eliminar Energía", "proyectos.energia.borrar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "ModuloId", "Nombre", "Slug" },
                values: new object[] { 3, "Ver Obra Civil", "proyectos.obracivil.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "ModuloId", "Nombre", "Slug" },
                values: new object[] { 3, "Crear Obra Civil", "proyectos.obracivil.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "ModuloId", "Nombre", "Slug" },
                values: new object[] { 3, "Editar Obra Civil", "proyectos.obracivil.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "ModuloId", "Nombre", "Slug" },
                values: new object[] { 3, "Eliminar Obra Civil", "proyectos.obracivil.borrar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Proyectos", 3, "Ver Telecomunicaciones", "proyectos.telecom.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Proyectos", 3, "Crear Telecomunicaciones", "proyectos.telecom.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Proyectos", 3, "Editar Telecomunicaciones", "proyectos.telecom.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Proyectos", 3, "Eliminar Telecomunicaciones", "proyectos.telecom.borrar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Finanzas", 4, "Ver Finanzas", "finanzas.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Finanzas", 4, "Crear Finanzas", "finanzas.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Finanzas", 4, "Editar Finanzas", "finanzas.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Finanzas", 4, "Eliminar Finanzas", "finanzas.borrar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Gestion Humana", 5, "Ver Gestión Humana", "gestionhumana.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Gestion Humana", 5, "Crear Gestión Humana", "gestionhumana.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Gestion Humana", 5, "Editar Gestión Humana", "gestionhumana.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "Gestion Humana", 5, "Eliminar Gestión Humana", "gestionhumana.borrar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "SST", 2, "Ver SST", "sst.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 38,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "SST", 2, "Crear SST", "sst.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 39,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "SST", 2, "Editar SST", "sst.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 40,
                columns: new[] { "Area", "ModuloId", "Nombre", "Slug" },
                values: new object[] { "SST", 2, "Eliminar SST", "sst.borrar" });

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_ModuloId",
                table: "Permisos",
                column: "ModuloId");

            migrationBuilder.AddForeignKey(
                name: "FK_Permisos_Modulos_ModuloId",
                table: "Permisos",
                column: "ModuloId",
                principalTable: "Modulos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permisos_Modulos_ModuloId",
                table: "Permisos");

            migrationBuilder.DropTable(
                name: "Modulos");

            migrationBuilder.DropIndex(
                name: "IX_Permisos_ModuloId",
                table: "Permisos");

            migrationBuilder.DropColumn(
                name: "ModuloId",
                table: "Permisos");

            migrationBuilder.AddColumn<string>(
                name: "Modulo",
                table: "Permisos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 1,
                column: "Modulo",
                value: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 2,
                column: "Modulo",
                value: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 3,
                column: "Modulo",
                value: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 4,
                column: "Modulo",
                value: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 5,
                column: "Modulo",
                value: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 6,
                column: "Modulo",
                value: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 7,
                column: "Modulo",
                value: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 8,
                column: "Modulo",
                value: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 9,
                column: "Modulo",
                value: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 10,
                column: "Modulo",
                value: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 11,
                column: "Modulo",
                value: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 12,
                column: "Modulo",
                value: "");

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Proyectos", "", "Ver Energía", "proyectos.energia.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Proyectos", "", "Crear Energía", "proyectos.energia.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Proyectos", "", "Editar Energía", "proyectos.energia.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Proyectos", "", "Eliminar Energía", "proyectos.energia.borrar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Modulo", "Nombre", "Slug" },
                values: new object[] { "", "Ver Obra Civil", "proyectos.obracivil.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Modulo", "Nombre", "Slug" },
                values: new object[] { "", "Crear Obra Civil", "proyectos.obracivil.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Modulo", "Nombre", "Slug" },
                values: new object[] { "", "Editar Obra Civil", "proyectos.obracivil.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Modulo", "Nombre", "Slug" },
                values: new object[] { "", "Eliminar Obra Civil", "proyectos.obracivil.borrar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Modulo", "Nombre", "Slug" },
                values: new object[] { "", "Ver Telecomunicaciones", "proyectos.telecom.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Modulo", "Nombre", "Slug" },
                values: new object[] { "", "Crear Telecomunicaciones", "proyectos.telecom.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "Modulo", "Nombre", "Slug" },
                values: new object[] { "", "Editar Telecomunicaciones", "proyectos.telecom.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "Modulo", "Nombre", "Slug" },
                values: new object[] { "", "Eliminar Telecomunicaciones", "proyectos.telecom.borrar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Finanzas", "", "Ver Finanzas", "finanzas.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Finanzas", "", "Crear Finanzas", "finanzas.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Finanzas", "", "Editar Finanzas", "finanzas.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Finanzas", "", "Eliminar Finanzas", "finanzas.borrar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Gestion Humana", "", "Ver Gestión Humana", "gestionhumana.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Gestion Humana", "", "Crear Gestión Humana", "gestionhumana.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Gestion Humana", "", "Editar Gestión Humana", "gestionhumana.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Gestion Humana", "", "Eliminar Gestión Humana", "gestionhumana.borrar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 33,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "SST", "", "Ver SST", "sst.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 34,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "SST", "", "Crear SST", "sst.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 35,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "SST", "", "Editar SST", "sst.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 36,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "SST", "", "Eliminar SST", "sst.borrar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 37,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Administracion", "", "Ver Catálogo de Permisos", "admin.permisos.leer" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 38,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Administracion", "", "Crear Catálogo de Permisos", "admin.permisos.crear" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 39,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Administracion", "", "Editar Catálogo de Permisos", "admin.permisos.editar" });

            migrationBuilder.UpdateData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 40,
                columns: new[] { "Area", "Modulo", "Nombre", "Slug" },
                values: new object[] { "Administracion", "", "Eliminar Catálogo de Permisos", "admin.permisos.borrar" });
        }
    }
}
