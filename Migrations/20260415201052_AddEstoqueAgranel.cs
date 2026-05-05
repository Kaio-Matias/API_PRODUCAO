using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PRODUCAO.Migrations
{
    /// <inheritdoc />
    public partial class AddEstoqueAgranel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EstoqueAgranel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoAgranel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DescricaoAgranel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SaldoLitros = table.Column<double>(type: "float", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UltimaAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observacoes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstoqueAgranel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovimentosEstoqueAgranel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoAgranel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TipoMovimento = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    QuantidadeLitros = table.Column<double>(type: "float", nullable: false),
                    SaldoAnterior = table.Column<double>(type: "float", nullable: false),
                    SaldoPosterior = table.Column<double>(type: "float", nullable: false),
                    DataMovimento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Observacao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Usuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimentosEstoqueAgranel", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EstoqueAgranel");

            migrationBuilder.DropTable(
                name: "MovimentosEstoqueAgranel");
        }
    }
}
