using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlicoNutri.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class HerancaTph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_alertas_pacientes_paciente_id",
                table: "alertas");

            migrationBuilder.DropForeignKey(
                name: "fk_necessidades_energeticas_pacientes_paciente_id",
                table: "necessidades_energeticas");

            migrationBuilder.DropForeignKey(
                name: "fk_nutricionista_paciente_nutricionistas_nutricionista_id",
                table: "nutricionista_paciente");

            migrationBuilder.DropForeignKey(
                name: "fk_nutricionista_paciente_pacientes_paciente_id",
                table: "nutricionista_paciente");

            migrationBuilder.DropForeignKey(
                name: "fk_planos_alimentares_nutricionistas_nutricionista_id",
                table: "planos_alimentares");

            migrationBuilder.DropForeignKey(
                name: "fk_planos_alimentares_pacientes_paciente_id",
                table: "planos_alimentares");

            migrationBuilder.DropForeignKey(
                name: "fk_receitas_nutricionistas_nutricionista_id",
                table: "receitas");

            migrationBuilder.DropForeignKey(
                name: "fk_registros_antropometricos_pacientes_paciente_id",
                table: "registros_antropometricos");

            migrationBuilder.DropForeignKey(
                name: "fk_registros_emocionais_pacientes_paciente_id",
                table: "registros_emocionais");

            migrationBuilder.DropForeignKey(
                name: "fk_registros_glicemia_pacientes_paciente_id",
                table: "registros_glicemia");

            migrationBuilder.DropForeignKey(
                name: "fk_resumo_clinico_paciente_pacientes_paciente_id",
                table: "resumo_clinico_paciente");

            migrationBuilder.AddColumn<string>(
                name: "cpf",
                table: "usuarios",
                type: "character varying(14)",
                maxLength: 14,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "crn",
                table: "usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "data_nascimento",
                table: "usuarios",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "especialidade",
                table: "usuarios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "glicemia_max_alvo",
                table: "usuarios",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "glicemia_min_alvo",
                table: "usuarios",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "medicacao_em_uso",
                table: "usuarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "nivel_acesso",
                table: "usuarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "observacoes_clinicas",
                table: "usuarios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "sexo_id",
                table: "usuarios",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telefone",
                table: "usuarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "tipo_diabetes_id",
                table: "usuarios",
                type: "bigint",
                nullable: true);

            // As colunas já existem e ainda estão vazias: os dados dos subtipos são
            // trazidos para usuarios ANTES de as tabelas de origem caírem. O EF
            // gera o DropTable primeiro, o que apagaria todo mundo.
            migrationBuilder.Sql("""
                UPDATE usuarios u SET
                    cpf                  = p.cpf,
                    data_nascimento      = p.data_nascimento,
                    sexo_id              = p.sexo_id,
                    tipo_diabetes_id     = p.tipo_diabetes_id,
                    medicacao_em_uso     = p.medicacao_em_uso,
                    observacoes_clinicas = p.observacoes_clinicas,
                    glicemia_min_alvo    = p.glicemia_min_alvo,
                    glicemia_max_alvo    = p.glicemia_max_alvo,
                    telefone             = p.telefone
                FROM pacientes p WHERE p.usuario_id = u.id;
            """);

            migrationBuilder.Sql("""
                UPDATE usuarios u SET
                    crn           = n.crn,
                    especialidade = n.especialidade,
                    telefone      = n.telefone
                FROM nutricionistas n WHERE n.usuario_id = u.id;
            """);

            migrationBuilder.Sql("""
                UPDATE usuarios u SET nivel_acesso = a.nivel_acesso
                FROM administradores a WHERE a.usuario_id = u.id;
            """);

            migrationBuilder.DropTable(
                name: "administradores");

            migrationBuilder.DropTable(
                name: "nutricionistas");

            migrationBuilder.DropTable(
                name: "pacientes");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_cpf",
                table: "usuarios",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_crn",
                table: "usuarios",
                column: "crn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_sexo_id",
                table: "usuarios",
                column: "sexo_id");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_tipo_diabetes_id",
                table: "usuarios",
                column: "tipo_diabetes_id");

            migrationBuilder.AddForeignKey(
                name: "fk_alertas_usuarios_paciente_id",
                table: "alertas",
                column: "paciente_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_necessidades_energeticas_usuarios_paciente_id",
                table: "necessidades_energeticas",
                column: "paciente_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_nutricionista_paciente_usuarios_nutricionista_id",
                table: "nutricionista_paciente",
                column: "nutricionista_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_nutricionista_paciente_usuarios_paciente_id",
                table: "nutricionista_paciente",
                column: "paciente_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_planos_alimentares_usuarios_nutricionista_id",
                table: "planos_alimentares",
                column: "nutricionista_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_planos_alimentares_usuarios_paciente_id",
                table: "planos_alimentares",
                column: "paciente_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receitas_usuarios_nutricionista_id",
                table: "receitas",
                column: "nutricionista_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registros_antropometricos_usuarios_paciente_id",
                table: "registros_antropometricos",
                column: "paciente_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registros_emocionais_usuarios_paciente_id",
                table: "registros_emocionais",
                column: "paciente_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registros_glicemia_usuarios_paciente_id",
                table: "registros_glicemia",
                column: "paciente_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_resumo_clinico_paciente_usuarios_paciente_id",
                table: "resumo_clinico_paciente",
                column: "paciente_id",
                principalTable: "usuarios",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_usuarios_sexos_biologicos_sexo_id",
                table: "usuarios",
                column: "sexo_id",
                principalTable: "sexos_biologicos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_usuarios_tipos_diabetes_tipo_diabetes_id",
                table: "usuarios",
                column: "tipo_diabetes_id",
                principalTable: "tipos_diabetes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            // O que era NOT NULL nas tabelas dos subtipos não pode mais ser: sob TPH
            // a coluna é nula nas linhas dos outros perfis. A obrigatoriedade passa a
            // valer condicionada ao perfil, que é o discriminador da hierarquia.
            migrationBuilder.Sql("""
                ALTER TABLE usuarios ADD CONSTRAINT ck_usuarios_paciente_completo CHECK (
                    perfil_id <> 1 OR (cpf IS NOT NULL AND data_nascimento IS NOT NULL
                                       AND sexo_id IS NOT NULL AND tipo_diabetes_id IS NOT NULL));
            """);

            migrationBuilder.Sql("""
                ALTER TABLE usuarios ADD CONSTRAINT ck_usuarios_nutricionista_completo CHECK (
                    perfil_id <> 2 OR crn IS NOT NULL);
            """);

            migrationBuilder.Sql("""
                ALTER TABLE usuarios ADD CONSTRAINT ck_usuarios_administrador_completo CHECK (
                    perfil_id <> 3 OR nivel_acesso IS NOT NULL);
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE usuarios DROP CONSTRAINT ck_usuarios_paciente_completo;");
            migrationBuilder.Sql("ALTER TABLE usuarios DROP CONSTRAINT ck_usuarios_nutricionista_completo;");
            migrationBuilder.Sql("ALTER TABLE usuarios DROP CONSTRAINT ck_usuarios_administrador_completo;");

            migrationBuilder.DropForeignKey(
                name: "fk_alertas_usuarios_paciente_id",
                table: "alertas");

            migrationBuilder.DropForeignKey(
                name: "fk_necessidades_energeticas_usuarios_paciente_id",
                table: "necessidades_energeticas");

            migrationBuilder.DropForeignKey(
                name: "fk_nutricionista_paciente_usuarios_nutricionista_id",
                table: "nutricionista_paciente");

            migrationBuilder.DropForeignKey(
                name: "fk_nutricionista_paciente_usuarios_paciente_id",
                table: "nutricionista_paciente");

            migrationBuilder.DropForeignKey(
                name: "fk_planos_alimentares_usuarios_nutricionista_id",
                table: "planos_alimentares");

            migrationBuilder.DropForeignKey(
                name: "fk_planos_alimentares_usuarios_paciente_id",
                table: "planos_alimentares");

            migrationBuilder.DropForeignKey(
                name: "fk_receitas_usuarios_nutricionista_id",
                table: "receitas");

            migrationBuilder.DropForeignKey(
                name: "fk_registros_antropometricos_usuarios_paciente_id",
                table: "registros_antropometricos");

            migrationBuilder.DropForeignKey(
                name: "fk_registros_emocionais_usuarios_paciente_id",
                table: "registros_emocionais");

            migrationBuilder.DropForeignKey(
                name: "fk_registros_glicemia_usuarios_paciente_id",
                table: "registros_glicemia");

            migrationBuilder.DropForeignKey(
                name: "fk_resumo_clinico_paciente_usuarios_paciente_id",
                table: "resumo_clinico_paciente");

            migrationBuilder.DropForeignKey(
                name: "fk_usuarios_sexos_biologicos_sexo_id",
                table: "usuarios");

            migrationBuilder.DropForeignKey(
                name: "fk_usuarios_tipos_diabetes_tipo_diabetes_id",
                table: "usuarios");

            migrationBuilder.DropIndex(
                name: "ix_usuarios_cpf",
                table: "usuarios");

            migrationBuilder.DropIndex(
                name: "ix_usuarios_crn",
                table: "usuarios");

            migrationBuilder.DropIndex(
                name: "ix_usuarios_sexo_id",
                table: "usuarios");

            migrationBuilder.DropIndex(
                name: "ix_usuarios_tipo_diabetes_id",
                table: "usuarios");

            migrationBuilder.CreateTable(
                name: "administradores",
                columns: table => new
                {
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    nivel_acesso = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_administradores", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_administradores_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nutricionistas",
                columns: table => new
                {
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    crn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    especialidade = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutricionistas", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_nutricionistas_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pacientes",
                columns: table => new
                {
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    sexo_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_diabetes_id = table.Column<long>(type: "bigint", nullable: false),
                    cpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    data_nascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    glicemia_max_alvo = table.Column<double>(type: "double precision", nullable: true),
                    glicemia_min_alvo = table.Column<double>(type: "double precision", nullable: true),
                    medicacao_em_uso = table.Column<string>(type: "text", nullable: true),
                    observacoes_clinicas = table.Column<string>(type: "text", nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pacientes", x => x.usuario_id);
                    table.ForeignKey(
                        name: "FK_pacientes_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_pacientes_sexos_biologicos_sexo_id",
                        column: x => x.sexo_id,
                        principalTable: "sexos_biologicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pacientes_tipos_diabetes_tipo_diabetes_id",
                        column: x => x.tipo_diabetes_id,
                        principalTable: "tipos_diabetes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO pacientes (usuario_id, cpf, data_nascimento, sexo_id, tipo_diabetes_id,
                                       medicacao_em_uso, observacoes_clinicas,
                                       glicemia_min_alvo, glicemia_max_alvo, telefone)
                SELECT id, cpf, data_nascimento, sexo_id, tipo_diabetes_id,
                       medicacao_em_uso, observacoes_clinicas,
                       glicemia_min_alvo, glicemia_max_alvo, telefone
                FROM usuarios WHERE perfil_id = 1;
            """);

            migrationBuilder.Sql("""
                INSERT INTO nutricionistas (usuario_id, crn, especialidade, telefone)
                SELECT id, crn, especialidade, telefone FROM usuarios WHERE perfil_id = 2;
            """);

            migrationBuilder.Sql("""
                INSERT INTO administradores (usuario_id, nivel_acesso)
                SELECT id, COALESCE(nivel_acesso, 1) FROM usuarios WHERE perfil_id = 3;
            """);

            migrationBuilder.DropColumn(
                name: "cpf",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "crn",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "data_nascimento",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "especialidade",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "glicemia_max_alvo",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "glicemia_min_alvo",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "medicacao_em_uso",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "nivel_acesso",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "observacoes_clinicas",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "sexo_id",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "telefone",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "tipo_diabetes_id",
                table: "usuarios");

            migrationBuilder.CreateIndex(
                name: "ix_nutricionistas_crn",
                table: "nutricionistas",
                column: "crn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pacientes_cpf",
                table: "pacientes",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pacientes_sexo_id",
                table: "pacientes",
                column: "sexo_id");

            migrationBuilder.CreateIndex(
                name: "ix_pacientes_tipo_diabetes_id",
                table: "pacientes",
                column: "tipo_diabetes_id");

            migrationBuilder.AddForeignKey(
                name: "fk_alertas_pacientes_paciente_id",
                table: "alertas",
                column: "paciente_id",
                principalTable: "pacientes",
                principalColumn: "usuario_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_necessidades_energeticas_pacientes_paciente_id",
                table: "necessidades_energeticas",
                column: "paciente_id",
                principalTable: "pacientes",
                principalColumn: "usuario_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_nutricionista_paciente_nutricionistas_nutricionista_id",
                table: "nutricionista_paciente",
                column: "nutricionista_id",
                principalTable: "nutricionistas",
                principalColumn: "usuario_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_nutricionista_paciente_pacientes_paciente_id",
                table: "nutricionista_paciente",
                column: "paciente_id",
                principalTable: "pacientes",
                principalColumn: "usuario_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_planos_alimentares_nutricionistas_nutricionista_id",
                table: "planos_alimentares",
                column: "nutricionista_id",
                principalTable: "nutricionistas",
                principalColumn: "usuario_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_planos_alimentares_pacientes_paciente_id",
                table: "planos_alimentares",
                column: "paciente_id",
                principalTable: "pacientes",
                principalColumn: "usuario_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_receitas_nutricionistas_nutricionista_id",
                table: "receitas",
                column: "nutricionista_id",
                principalTable: "nutricionistas",
                principalColumn: "usuario_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registros_antropometricos_pacientes_paciente_id",
                table: "registros_antropometricos",
                column: "paciente_id",
                principalTable: "pacientes",
                principalColumn: "usuario_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registros_emocionais_pacientes_paciente_id",
                table: "registros_emocionais",
                column: "paciente_id",
                principalTable: "pacientes",
                principalColumn: "usuario_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_registros_glicemia_pacientes_paciente_id",
                table: "registros_glicemia",
                column: "paciente_id",
                principalTable: "pacientes",
                principalColumn: "usuario_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_resumo_clinico_paciente_pacientes_paciente_id",
                table: "resumo_clinico_paciente",
                column: "paciente_id",
                principalTable: "pacientes",
                principalColumn: "usuario_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
