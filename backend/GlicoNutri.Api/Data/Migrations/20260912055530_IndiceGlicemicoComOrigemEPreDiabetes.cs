using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GlicoNutri.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndiceGlicemicoComOrigemEPreDiabetes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "fonte_indice_glicemico_id",
                table: "alimentos",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "fontes_indice_glicemico",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fontes_indice_glicemico", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "fontes_indice_glicemico",
                columns: new[] { "id", "ativo", "codigo", "descricao" },
                values: new object[,]
                {
                    { 1L, true, "TABELA_INTERNACIONAL", "International Tables of Glycemic Index and Glycemic Load Values 2021" },
                    { 2L, true, "PROFISSIONAL", "Informado pelo nutricionista" }
                });

            migrationBuilder.UpdateData(
                table: "formulas_energeticas",
                keyColumn: "id",
                keyValue: 1L,
                column: "descricao",
                value: "Harris-Benedict (revisão de Roza e Shizgal, 1984)");

            migrationBuilder.InsertData(
                table: "tipos_diabetes",
                columns: new[] { "id", "ativo", "codigo", "descricao" },
                values: new object[] { 5L, true, "PRE_DIABETES", "Pré-diabetes" });

            migrationBuilder.CreateIndex(
                name: "ix_alimentos_fonte_indice_glicemico_id",
                table: "alimentos",
                column: "fonte_indice_glicemico_id");

            migrationBuilder.CreateIndex(
                name: "ix_fontes_indice_glicemico_codigo",
                table: "fontes_indice_glicemico",
                column: "codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_alimentos_fontes_indice_glicemico_fonte_indice_glicemico_id",
                table: "alimentos",
                column: "fonte_indice_glicemico_id",
                principalTable: "fontes_indice_glicemico",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            // As tabelas de referência substituíram os enums e passam a aceitar
            // cadastro. Sem isto "Glicose", "glicose" e "GLICOSE" conviveriam como
            // três linhas distintas, e o select do formulário mostraria as três.
            //
            // translate em vez de unaccent: a função precisa ser IMMUTABLE para
            // entrar em índice, e unaccent depende de um dicionário de busca que
            // nem sempre está no search_path — nos bancos descartáveis dos testes
            // de integração, por exemplo, ele não resolve. translate é builtin,
            // imutável e cobre o que aparece em rótulo de referência em português.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION normalizar_referencia(texto text)
                RETURNS text LANGUAGE sql IMMUTABLE STRICT AS $$
                    SELECT lower(btrim(translate(texto,
                        'áàâãäéèêëíìîïóòôõöúùûüñçÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÑÇ',
                        'aaaaaeeeeiiiiooooouuuuncAAAAAEEEEIIIIOOOOOUUUUNC')))
                $$;
            """);

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_sexos_biologicos_descricao_normalizada " +
                "ON sexos_biologicos (normalizar_referencia(descricao));");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_tipos_diabetes_descricao_normalizada " +
                "ON tipos_diabetes (normalizar_referencia(descricao));");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_perfis_usuario_descricao_normalizada " +
                "ON perfis_usuario (normalizar_referencia(descricao));");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_contextos_glicemia_descricao_normalizada " +
                "ON contextos_glicemia (normalizar_referencia(descricao));");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_estados_emocionais_descricao_normalizada " +
                "ON estados_emocionais (normalizar_referencia(descricao));");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_formulas_energeticas_descricao_normalizada " +
                "ON formulas_energeticas (normalizar_referencia(descricao));");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_niveis_atividade_descricao_normalizada " +
                "ON niveis_atividade (normalizar_referencia(descricao));");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_tipos_conteudo_descricao_normalizada " +
                "ON tipos_conteudo (normalizar_referencia(descricao));");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_tipos_alerta_descricao_normalizada " +
                "ON tipos_alerta (normalizar_referencia(descricao));");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_status_envio_descricao_normalizada " +
                "ON status_envio (normalizar_referencia(descricao));");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_fontes_alimento_descricao_normalizada " +
                "ON fontes_alimento (normalizar_referencia(descricao));");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ux_fontes_indice_glicemico_descricao_normalizada " +
                "ON fontes_indice_glicemico (normalizar_referencia(descricao));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX ux_sexos_biologicos_descricao_normalizada;");
            migrationBuilder.Sql("DROP INDEX ux_tipos_diabetes_descricao_normalizada;");
            migrationBuilder.Sql("DROP INDEX ux_perfis_usuario_descricao_normalizada;");
            migrationBuilder.Sql("DROP INDEX ux_contextos_glicemia_descricao_normalizada;");
            migrationBuilder.Sql("DROP INDEX ux_estados_emocionais_descricao_normalizada;");
            migrationBuilder.Sql("DROP INDEX ux_formulas_energeticas_descricao_normalizada;");
            migrationBuilder.Sql("DROP INDEX ux_niveis_atividade_descricao_normalizada;");
            migrationBuilder.Sql("DROP INDEX ux_tipos_conteudo_descricao_normalizada;");
            migrationBuilder.Sql("DROP INDEX ux_tipos_alerta_descricao_normalizada;");
            migrationBuilder.Sql("DROP INDEX ux_status_envio_descricao_normalizada;");
            migrationBuilder.Sql("DROP INDEX ux_fontes_alimento_descricao_normalizada;");
            migrationBuilder.Sql("DROP INDEX ux_fontes_indice_glicemico_descricao_normalizada;");
            migrationBuilder.Sql("DROP FUNCTION normalizar_referencia(text);");

            migrationBuilder.DropForeignKey(
                name: "fk_alimentos_fontes_indice_glicemico_fonte_indice_glicemico_id",
                table: "alimentos");

            migrationBuilder.DropTable(
                name: "fontes_indice_glicemico");

            migrationBuilder.DropIndex(
                name: "ix_alimentos_fonte_indice_glicemico_id",
                table: "alimentos");

            migrationBuilder.DeleteData(
                table: "tipos_diabetes",
                keyColumn: "id",
                keyValue: 5L);

            migrationBuilder.DropColumn(
                name: "fonte_indice_glicemico_id",
                table: "alimentos");

            migrationBuilder.UpdateData(
                table: "formulas_energeticas",
                keyColumn: "id",
                keyValue: 1L,
                column: "descricao",
                value: "Harris-Benedict");
        }
    }
}
