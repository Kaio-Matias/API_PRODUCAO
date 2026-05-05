using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PRODUCAO.Migrations
{
    /// <inheritdoc />
    public partial class AddModulosAcesso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModulosAcesso",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModulosAcesso",
                table: "Usuarios");
        }
    }
}
