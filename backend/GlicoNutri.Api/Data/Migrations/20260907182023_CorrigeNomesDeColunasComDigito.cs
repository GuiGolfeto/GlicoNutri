using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlicoNutri.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CorrigeNomesDeColunasComDigito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "percentual_no_alvo7_dias",
                table: "resumo_clinico_paciente",
                newName: "percentual_no_alvo_7dias");

            migrationBuilder.RenameColumn(
                name: "media_glicemia7_dias",
                table: "resumo_clinico_paciente",
                newName: "media_glicemia_7dias");

            migrationBuilder.RenameColumn(
                name: "proteinas_por100g",
                table: "alimentos",
                newName: "proteinas_por_100g");

            migrationBuilder.RenameColumn(
                name: "lipidios_por100g",
                table: "alimentos",
                newName: "lipidios_por_100g");

            migrationBuilder.RenameColumn(
                name: "fibras_por100g",
                table: "alimentos",
                newName: "fibras_por_100g");

            migrationBuilder.RenameColumn(
                name: "carboidratos_por100g",
                table: "alimentos",
                newName: "carboidratos_por_100g");

            migrationBuilder.RenameColumn(
                name: "calorias_por100g",
                table: "alimentos",
                newName: "calorias_por_100g");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "percentual_no_alvo_7dias",
                table: "resumo_clinico_paciente",
                newName: "percentual_no_alvo7_dias");

            migrationBuilder.RenameColumn(
                name: "media_glicemia_7dias",
                table: "resumo_clinico_paciente",
                newName: "media_glicemia7_dias");

            migrationBuilder.RenameColumn(
                name: "proteinas_por_100g",
                table: "alimentos",
                newName: "proteinas_por100g");

            migrationBuilder.RenameColumn(
                name: "lipidios_por_100g",
                table: "alimentos",
                newName: "lipidios_por100g");

            migrationBuilder.RenameColumn(
                name: "fibras_por_100g",
                table: "alimentos",
                newName: "fibras_por100g");

            migrationBuilder.RenameColumn(
                name: "carboidratos_por_100g",
                table: "alimentos",
                newName: "carboidratos_por100g");

            migrationBuilder.RenameColumn(
                name: "calorias_por_100g",
                table: "alimentos",
                newName: "calorias_por100g");
        }
    }
}
