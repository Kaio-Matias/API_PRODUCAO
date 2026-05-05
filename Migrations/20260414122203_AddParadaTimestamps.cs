using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_PRODUCAO.Migrations
{
    /// <inheritdoc />
    public partial class AddParadaTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Tempo",
                table: "Eficiencia",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AddColumn<DateTime>(
                name: "DataHoraFim",
                table: "Eficiencia",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataHoraInicio",
                table: "Eficiencia",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataHoraFim",
                table: "Eficiencia");

            migrationBuilder.DropColumn(
                name: "DataHoraInicio",
                table: "Eficiencia");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Tempo",
                table: "Eficiencia",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(TimeSpan),
                oldType: "time",
                oldNullable: true);
        }
    }
}
