using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PRODUCAO.Migrations
{
    /// <inheritdoc />
    public partial class MigraçãoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cadastros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodProduto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Produto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodBarra = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Maquina = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unidade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QtdeCaixa = table.Column<int>(type: "int", nullable: false),
                    QtdePorPalete = table.Column<int>(type: "int", nullable: false),
                    PesoBruto = table.Column<double>(type: "float", nullable: false),
                    PesoLiquido = table.Column<double>(type: "float", nullable: false),
                    PesoTotalPalete = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cadastros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Producoes",
                columns: table => new
                {
                    OrdemProducao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Produto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Maquina = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataHoraAbertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataHoraFechamento = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Producoes", x => x.OrdemProducao);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Matricula = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cadastros");

            migrationBuilder.DropTable(
                name: "Producoes");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
