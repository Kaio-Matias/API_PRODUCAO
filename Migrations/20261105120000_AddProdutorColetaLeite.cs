using API_PRODUCAO.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PRODUCAO.Migrations
{
    /// <summary>Coleta de leite passa a guardar o(s) produtor(es) da viagem.</summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261105120000_AddProdutorColetaLeite")]
    public partial class AddProdutorColetaLeite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Produtor",
                table: "ColetasLeite",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Produtor", table: "ColetasLeite");
        }
    }
}
