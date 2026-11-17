using API_PRODUCAO.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PRODUCAO.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261009190000_AddColetasLeite")]
    public partial class AddColetasLeite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ColetasLeite",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroColeta = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DataHoraColeta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlacaCaminhao = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Transportador = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Linha = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VolumeDescarregado = table.Column<double>(type: "float", nullable: false),
                    VolumeDetalhado = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColetasLeite", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ColetasLeite_NumeroColeta",
                table: "ColetasLeite",
                column: "NumeroColeta",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ColetasLeite");
        }
    }
}
