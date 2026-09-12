using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlicoNutri.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class FavoritosEMidiaDoConteudo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "url_midia",
                table: "conteudos_educativos",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "favoritos_conteudo",
                columns: table => new
                {
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    conteudo_id = table.Column<long>(type: "bigint", nullable: false),
                    data_favoritado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favoritos_conteudo", x => new { x.paciente_id, x.conteudo_id });
                    table.ForeignKey(
                        name: "fk_favoritos_conteudo_conteudos_educativos_conteudo_id",
                        column: x => x.conteudo_id,
                        principalTable: "conteudos_educativos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_favoritos_conteudo_usuarios_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "favoritos_receita",
                columns: table => new
                {
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    receita_id = table.Column<long>(type: "bigint", nullable: false),
                    data_favoritado = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_favoritos_receita", x => new { x.paciente_id, x.receita_id });
                    table.ForeignKey(
                        name: "fk_favoritos_receita_receitas_receita_id",
                        column: x => x.receita_id,
                        principalTable: "receitas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_favoritos_receita_usuarios_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_favoritos_conteudo_conteudo_id",
                table: "favoritos_conteudo",
                column: "conteudo_id");

            migrationBuilder.CreateIndex(
                name: "ix_favoritos_receita_receita_id",
                table: "favoritos_receita",
                column: "receita_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "favoritos_conteudo");

            migrationBuilder.DropTable(
                name: "favoritos_receita");

            migrationBuilder.DropColumn(
                name: "url_midia",
                table: "conteudos_educativos");
        }
    }
}
