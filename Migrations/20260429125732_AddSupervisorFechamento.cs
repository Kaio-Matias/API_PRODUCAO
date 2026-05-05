using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PRODUCAO.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorFechamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SupervisorFechamento",
                table: "Producoes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupervisorFechamento",
                table: "Producoes");
        }
    }
}
