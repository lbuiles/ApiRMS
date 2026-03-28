using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RmsErp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPermisoCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Area", "Modulo", "Nombre", "Slug" },
                values: new object[,]
                {
                    { 37, "Administracion", "", "Ver Catálogo de Permisos", "admin.permisos.leer" },
                    { 38, "Administracion", "", "Crear Catálogo de Permisos", "admin.permisos.crear" },
                    { 39, "Administracion", "", "Editar Catálogo de Permisos", "admin.permisos.editar" },
                    { 40, "Administracion", "", "Eliminar Catálogo de Permisos", "admin.permisos.borrar" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: 40);
        }
    }
}
