using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PRODUCAO.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreat : Migration
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
                    Unidade = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    Cargo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Matricula = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DetalhamentoOPs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrdemProducao = table.Column<int>(type: "int", nullable: false),
                    Operador = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Turno = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmbProcessadas = table.Column<int>(type: "int", nullable: false),
                    EmbProduzidas = table.Column<int>(type: "int", nullable: false),
                    EmbPerdidas = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalhamentoOPs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetalhamentoOPs_Producoes_OrdemProducao",
                        column: x => x.OrdemProducao,
                        principalTable: "Producoes",
                        principalColumn: "OrdemProducao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Eficiencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrdemProducao = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tempo = table.Column<TimeSpan>(type: "time", nullable: false),
                    Operador = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProducoesOrdemProducao = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eficiencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Eficiencia_Producoes_ProducoesOrdemProducao",
                        column: x => x.ProducoesOrdemProducao,
                        principalTable: "Producoes",
                        principalColumn: "OrdemProducao");
                });

            migrationBuilder.CreateTable(
                name: "Paletizacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    N_Palete = table.Column<int>(type: "int", nullable: false),
                    OrdemProducao = table.Column<int>(type: "int", nullable: false),
                    CodigoProduto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Produto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unidade = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Maquina = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Usuario = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QtdeCx = table.Column<int>(type: "int", nullable: false),
                    QtdePorPalete = table.Column<int>(type: "int", nullable: false),
                    QtdeProduzida = table.Column<int>(type: "int", nullable: false),
                    Bloqueio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataHoraPaletizacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProducoesOrdemProducao = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paletizacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Paletizacoes_Producoes_ProducoesOrdemProducao",
                        column: x => x.ProducoesOrdemProducao,
                        principalTable: "Producoes",
                        principalColumn: "OrdemProducao");
                });

            migrationBuilder.CreateTable(
                name: "Perdas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrdemProducao = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantidade = table.Column<int>(type: "int", nullable: false),
                    Operador = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perdas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perdas_Producoes_OrdemProducao",
                        column: x => x.OrdemProducao,
                        principalTable: "Producoes",
                        principalColumn: "OrdemProducao",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetalhamentoOPs_OrdemProducao",
                table: "DetalhamentoOPs",
                column: "OrdemProducao");

            migrationBuilder.CreateIndex(
                name: "IX_Eficiencia_ProducoesOrdemProducao",
                table: "Eficiencia",
                column: "ProducoesOrdemProducao");

            migrationBuilder.CreateIndex(
                name: "IX_Paletizacoes_ProducoesOrdemProducao",
                table: "Paletizacoes",
                column: "ProducoesOrdemProducao");

            migrationBuilder.CreateIndex(
                name: "IX_Perdas_OrdemProducao",
                table: "Perdas",
                column: "OrdemProducao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cadastros");

            migrationBuilder.DropTable(
                name: "DetalhamentoOPs");

            migrationBuilder.DropTable(
                name: "Eficiencia");

            migrationBuilder.DropTable(
                name: "Paletizacoes");

            migrationBuilder.DropTable(
                name: "Perdas");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Producoes");
        }
    }
}
