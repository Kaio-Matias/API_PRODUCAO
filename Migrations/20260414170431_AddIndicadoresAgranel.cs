using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PRODUCAO.Migrations
{
    /// <inheritdoc />
    public partial class AddIndicadoresAgranel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndicadoresAgranel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataReferencia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CodigoAgranel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DescricaoAgranel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Inicial = table.Column<double>(type: "float", nullable: false),
                    Preparado = table.Column<double>(type: "float", nullable: false),
                    Consumo = table.Column<double>(type: "float", nullable: false),
                    Final = table.Column<double>(type: "float", nullable: false),
                    Perdas = table.Column<double>(type: "float", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PerdaValorizada = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PercentualPerda = table.Column<double>(type: "float", nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicadoresAgranel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VinculosAgranelAcabado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoAgranel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DescricaoAgranel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodigoProdutoAcabado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DescricaoProdutoAcabado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LitrosPorCaixa = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VinculosAgranelAcabado", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndicadoresAgranel");

            migrationBuilder.DropTable(
                name: "VinculosAgranelAcabado");
        }
    }
}
