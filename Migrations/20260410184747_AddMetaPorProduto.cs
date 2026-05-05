using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PRODUCAO.Migrations
{
    /// <inheritdoc />
    public partial class AddMetaPorProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodProduto",
                table: "MetasProducao",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodProduto",
                table: "MetasProducao");
        }
    }
}
