using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlicoNutri.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenomearColunasHorarioAlerta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "horario2",
                table: "alertas",
                newName: "horario_2");

            migrationBuilder.RenameColumn(
                name: "horario1",
                table: "alertas",
                newName: "horario_1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "horario_2",
                table: "alertas",
                newName: "horario2");

            migrationBuilder.RenameColumn(
                name: "horario_1",
                table: "alertas",
                newName: "horario1");
        }
    }
}
