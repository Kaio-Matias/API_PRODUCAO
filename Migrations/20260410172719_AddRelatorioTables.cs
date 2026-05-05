using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PRODUCAO.Migrations
{
    /// <inheritdoc />
    public partial class AddRelatorioTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Classe",
                table: "Cadastros",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecoUnitario",
                table: "Cadastros",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CaptacoesLeite",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataRecebimento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VolumeRecebido = table.Column<double>(type: "float", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaptacoesLeite", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetasCaptacaoLeite",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    MetaLitros = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetasCaptacaoLeite", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetasProducao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    Classe = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetaCaixas = table.Column<double>(type: "float", nullable: false),
                    MetaKg = table.Column<double>(type: "float", nullable: false),
                    MetaRS = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetasProducao", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaptacoesLeite");

            migrationBuilder.DropTable(
                name: "MetasCaptacaoLeite");

            migrationBuilder.DropTable(
                name: "MetasProducao");

            migrationBuilder.DropColumn(
                name: "Classe",
                table: "Cadastros");

            migrationBuilder.DropColumn(
                name: "PrecoUnitario",
                table: "Cadastros");
        }
    }
}
