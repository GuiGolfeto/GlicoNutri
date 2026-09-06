using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GlicoNutri.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InicialDerV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contextos_glicemia",
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
                    table.PrimaryKey("PK_contextos_glicemia", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "estados_emocionais",
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
                    table.PrimaryKey("PK_estados_emocionais", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "fontes_alimento",
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
                    table.PrimaryKey("PK_fontes_alimento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "formulas_energeticas",
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
                    table.PrimaryKey("PK_formulas_energeticas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "niveis_atividade",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    fator = table.Column<double>(type: "double precision", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_niveis_atividade", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "perfis_usuario",
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
                    table.PrimaryKey("PK_perfis_usuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sexos_biologicos",
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
                    table.PrimaryKey("PK_sexos_biologicos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "status_envio",
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
                    table.PrimaryKey("PK_status_envio", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tipos_alerta",
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
                    table.PrimaryKey("PK_tipos_alerta", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tipos_conteudo",
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
                    table.PrimaryKey("PK_tipos_conteudo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tipos_diabetes",
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
                    table.PrimaryKey("PK_tipos_diabetes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alimentos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    grupo_alimentar = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    calorias_por100g = table.Column<double>(type: "double precision", nullable: true),
                    carboidratos_por100g = table.Column<double>(type: "double precision", nullable: true),
                    proteinas_por100g = table.Column<double>(type: "double precision", nullable: true),
                    lipidios_por100g = table.Column<double>(type: "double precision", nullable: true),
                    fibras_por100g = table.Column<double>(type: "double precision", nullable: true),
                    indice_glicemico = table.Column<int>(type: "integer", nullable: true),
                    fonte_id = table.Column<long>(type: "bigint", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alimentos", x => x.id);
                    table.ForeignKey(
                        name: "fk_alimentos_fontes_alimento_fonte_id",
                        column: x => x.fonte_id,
                        principalTable: "fontes_alimento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    senha_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    perfil_id = table.Column<long>(type: "bigint", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    data_cadastro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ultimo_acesso = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tentativas_invalidas = table.Column<int>(type: "integer", nullable: false),
                    nivel_bloqueio = table.Column<int>(type: "integer", nullable: false),
                    bloqueado_ate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    senha_provisoria = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_usuarios_perfis_usuario_perfil_id",
                        column: x => x.perfil_id,
                        principalTable: "perfis_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "conteudos_educativos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    autor_id = table.Column<long>(type: "bigint", nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tipo_id = table.Column<long>(type: "bigint", nullable: false),
                    corpo = table.Column<string>(type: "text", nullable: true),
                    data_publicacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conteudos_educativos", x => x.id);
                    table.ForeignKey(
                        name: "fk_conteudos_educativos_tipos_conteudo_tipo_id",
                        column: x => x.tipo_id,
                        principalTable: "tipos_conteudo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_conteudos_educativos_usuarios_autor_id",
                        column: x => x.autor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
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
                    cpf = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    sexo_id = table.Column<long>(type: "bigint", nullable: false),
                    data_nascimento = table.Column<DateOnly>(type: "date", nullable: false),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    tipo_diabetes_id = table.Column<long>(type: "bigint", nullable: false),
                    medicacao_em_uso = table.Column<string>(type: "text", nullable: true),
                    observacoes_clinicas = table.Column<string>(type: "text", nullable: true),
                    glicemia_min_alvo = table.Column<double>(type: "double precision", nullable: true),
                    glicemia_max_alvo = table.Column<double>(type: "double precision", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "receitas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nutricionista_id = table.Column<long>(type: "bigint", nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    tempo_preparo = table.Column<int>(type: "integer", nullable: true),
                    porcoes = table.Column<int>(type: "integer", nullable: true),
                    instrucoes = table.Column<string>(type: "text", nullable: true),
                    data_publicacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receitas", x => x.id);
                    table.ForeignKey(
                        name: "fk_receitas_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "alertas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo_id = table.Column<long>(type: "bigint", nullable: false),
                    mensagem = table.Column<string>(type: "text", nullable: true),
                    horario1 = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    horario2 = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    dias_semana = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alertas", x => x.id);
                    table.ForeignKey(
                        name: "fk_alertas_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_alertas_tipos_alerta_tipo_id",
                        column: x => x.tipo_id,
                        principalTable: "tipos_alerta",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "necessidades_energeticas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    formula_id = table.Column<long>(type: "bigint", nullable: false),
                    nivel_atividade_id = table.Column<long>(type: "bigint", nullable: false),
                    objetivo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    data_calculo = table.Column<DateOnly>(type: "date", nullable: false),
                    valor_kcal = table.Column<double>(type: "double precision", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_necessidades_energeticas", x => x.id);
                    table.ForeignKey(
                        name: "fk_necessidades_energeticas_formulas_energeticas_formula_id",
                        column: x => x.formula_id,
                        principalTable: "formulas_energeticas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_necessidades_energeticas_niveis_atividade_nivel_atividade_id",
                        column: x => x.nivel_atividade_id,
                        principalTable: "niveis_atividade",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_necessidades_energeticas_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nutricionista_paciente",
                columns: table => new
                {
                    nutricionista_id = table.Column<long>(type: "bigint", nullable: false),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    data_vinculo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nutricionista_paciente", x => new { x.nutricionista_id, x.paciente_id });
                    table.ForeignKey(
                        name: "fk_nutricionista_paciente_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nutricionista_paciente_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "planos_alimentares",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    nutricionista_id = table.Column<long>(type: "bigint", nullable: false),
                    data_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    data_fim = table.Column<DateOnly>(type: "date", nullable: true),
                    objetivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    observacoes = table.Column<string>(type: "text", nullable: true),
                    total_calorias_prescritas = table.Column<double>(type: "double precision", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planos_alimentares", x => x.id);
                    table.ForeignKey(
                        name: "fk_planos_alimentares_nutricionistas_nutricionista_id",
                        column: x => x.nutricionista_id,
                        principalTable: "nutricionistas",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_planos_alimentares_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registros_antropometricos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    peso = table.Column<double>(type: "double precision", nullable: false),
                    altura = table.Column<double>(type: "double precision", nullable: false),
                    circunferencia_abdominal = table.Column<double>(type: "double precision", nullable: true),
                    circunferencia_cintura = table.Column<double>(type: "double precision", nullable: true),
                    circunferencia_quadril = table.Column<double>(type: "double precision", nullable: true),
                    circunferencia_braco = table.Column<double>(type: "double precision", nullable: true),
                    rcq = table.Column<double>(type: "double precision", nullable: true),
                    imc = table.Column<double>(type: "double precision", nullable: true),
                    classificacao_imc = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registros_antropometricos", x => x.id);
                    table.ForeignKey(
                        name: "fk_registros_antropometricos_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registros_emocionais",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    estado_emocional_id = table.Column<long>(type: "bigint", nullable: false),
                    intensidade = table.Column<int>(type: "integer", nullable: false),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    descricao = table.Column<string>(type: "text", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registros_emocionais", x => x.id);
                    table.CheckConstraint("ck_registros_emocionais_intensidade", "intensidade BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_registros_emocionais_estados_emocionais_estado_emocional_id",
                        column: x => x.estado_emocional_id,
                        principalTable: "estados_emocionais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registros_emocionais_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "registros_glicemia",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    valor = table.Column<double>(type: "double precision", nullable: false),
                    contexto_id = table.Column<long>(type: "bigint", nullable: false),
                    data_hora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    observacao = table.Column<string>(type: "text", nullable: true),
                    fora_do_alvo = table.Column<bool>(type: "boolean", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registros_glicemia", x => x.id);
                    table.CheckConstraint("ck_registros_glicemia_valor", "valor >= 0 AND valor <= 600");
                    table.ForeignKey(
                        name: "fk_registros_glicemia_contextos_glicemia_contexto_id",
                        column: x => x.contexto_id,
                        principalTable: "contextos_glicemia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_registros_glicemia_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "resumo_clinico_paciente",
                columns: table => new
                {
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    ultima_glicemia_valor = table.Column<double>(type: "double precision", nullable: true),
                    ultima_glicemia_contexto_id = table.Column<long>(type: "bigint", nullable: true),
                    ultima_glicemia_data = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    media_glicemia7_dias = table.Column<double>(type: "double precision", nullable: true),
                    percentual_no_alvo7_dias = table.Column<double>(type: "double precision", nullable: true),
                    ultimo_imc = table.Column<double>(type: "double precision", nullable: true),
                    ultima_classificacao_imc = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ultima_data_antropometria = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    plano_ativo = table.Column<bool>(type: "boolean", nullable: false),
                    alertas_pendentes_count = table.Column<int>(type: "integer", nullable: false),
                    dias_sem_registro_glicemia = table.Column<int>(type: "integer", nullable: true),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resumo_clinico_paciente", x => x.paciente_id);
                    table.ForeignKey(
                        name: "fk_resumo_clinico_paciente_contextos_glicemia_ultima_glicemia_~",
                        column: x => x.ultima_glicemia_contexto_id,
                        principalTable: "contextos_glicemia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_resumo_clinico_paciente_pacientes_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "pacientes",
                        principalColumn: "usuario_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ingredientes_receita",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    receita_id = table.Column<long>(type: "bigint", nullable: false),
                    alimento_id = table.Column<long>(type: "bigint", nullable: false),
                    quantidade = table.Column<double>(type: "double precision", nullable: false),
                    unidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ingredientes_receita", x => x.id);
                    table.ForeignKey(
                        name: "fk_ingredientes_receita_alimentos_alimento_id",
                        column: x => x.alimento_id,
                        principalTable: "alimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ingredientes_receita_receitas_receita_id",
                        column: x => x.receita_id,
                        principalTable: "receitas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "historico_alertas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    alerta_id = table.Column<long>(type: "bigint", nullable: false),
                    data_hora_disparo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    data_hora_atendimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status_envio_id = table.Column<long>(type: "bigint", nullable: false),
                    tentativas = table.Column<int>(type: "integer", nullable: false),
                    mensagem_erro = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historico_alertas", x => x.id);
                    table.ForeignKey(
                        name: "fk_historico_alertas_alertas_alerta_id",
                        column: x => x.alerta_id,
                        principalTable: "alertas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_historico_alertas_status_envio_status_envio_id",
                        column: x => x.status_envio_id,
                        principalTable: "status_envio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "distribuicao_macronutrientes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plano_alimentar_id = table.Column<long>(type: "bigint", nullable: false),
                    carboidratos_percentual = table.Column<double>(type: "double precision", nullable: false),
                    proteinas_percentual = table.Column<double>(type: "double precision", nullable: false),
                    lipidios_percentual = table.Column<double>(type: "double precision", nullable: false),
                    total_calorias_prescritas = table.Column<double>(type: "double precision", nullable: true),
                    carboidratos_gramas = table.Column<double>(type: "double precision", nullable: true),
                    proteinas_gramas = table.Column<double>(type: "double precision", nullable: true),
                    lipidios_gramas = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_distribuicao_macronutrientes", x => x.id);
                    table.ForeignKey(
                        name: "fk_distribuicao_macronutrientes_planos_alimentares_plano_alime~",
                        column: x => x.plano_alimentar_id,
                        principalTable: "planos_alimentares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itens_plano_alimentar",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plano_alimentar_id = table.Column<long>(type: "bigint", nullable: false),
                    alimento_id = table.Column<long>(type: "bigint", nullable: false),
                    dia = table.Column<int>(type: "integer", nullable: false),
                    horario = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    refeicao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    quantidade = table.Column<double>(type: "double precision", nullable: false),
                    unidade = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itens_plano_alimentar", x => x.id);
                    table.ForeignKey(
                        name: "fk_itens_plano_alimentar_alimentos_alimento_id",
                        column: x => x.alimento_id,
                        principalTable: "alimentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_itens_plano_alimentar_planos_alimentares_plano_alimentar_id",
                        column: x => x.plano_alimentar_id,
                        principalTable: "planos_alimentares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "contextos_glicemia",
                columns: new[] { "id", "ativo", "codigo", "descricao" },
                values: new object[,]
                {
                    { 1L, true, "JEJUM", "Jejum" },
                    { 2L, true, "PRE_REFEICAO", "Pré-refeição" },
                    { 3L, true, "POS_REFEICAO", "Pós-refeição" },
                    { 4L, true, "ANTES_DE_DORMIR", "Ao deitar" },
                    { 5L, true, "OUTRO", "Outro" }
                });

            migrationBuilder.InsertData(
                table: "estados_emocionais",
                columns: new[] { "id", "ativo", "codigo", "descricao" },
                values: new object[,]
                {
                    { 1L, true, "FELIZ", "Feliz" },
                    { 2L, true, "TRANQUILO", "Tranquilo" },
                    { 3L, true, "ANSIOSO", "Ansioso" },
                    { 4L, true, "TRISTE", "Triste" },
                    { 5L, true, "IRRITADO", "Irritado" },
                    { 6L, true, "ESTRESSADO", "Estressado" }
                });

            migrationBuilder.InsertData(
                table: "fontes_alimento",
                columns: new[] { "id", "ativo", "codigo", "descricao" },
                values: new object[,]
                {
                    { 1L, true, "TACO", "Tabela TACO / IBGE" },
                    { 2L, true, "MANUAL", "Cadastro manual" }
                });

            migrationBuilder.InsertData(
                table: "formulas_energeticas",
                columns: new[] { "id", "ativo", "codigo", "descricao" },
                values: new object[,]
                {
                    { 1L, true, "HARRIS_BENEDICT", "Harris-Benedict" },
                    { 2L, true, "MIFFLIN_ST_JEOR", "Mifflin-St Jeor" }
                });

            migrationBuilder.InsertData(
                table: "niveis_atividade",
                columns: new[] { "id", "ativo", "codigo", "descricao", "fator" },
                values: new object[,]
                {
                    { 1L, true, "SEDENTARIO", "Sedentário", 1.2 },
                    { 2L, true, "LEVEMENTE_ATIVO", "Levemente ativo", 1.375 },
                    { 3L, true, "MODERADAMENTE_ATIVO", "Moderadamente ativo", 1.55 },
                    { 4L, true, "MUITO_ATIVO", "Muito ativo", 1.7250000000000001 },
                    { 5L, true, "EXTREMAMENTE_ATIVO", "Extremamente ativo", 1.8999999999999999 }
                });

            migrationBuilder.InsertData(
                table: "perfis_usuario",
                columns: new[] { "id", "ativo", "codigo", "descricao" },
                values: new object[,]
                {
                    { 1L, true, "PACIENTE", "Paciente" },
                    { 2L, true, "NUTRICIONISTA", "Nutricionista" },
                    { 3L, true, "ADMINISTRADOR", "Administrador" }
                });

            migrationBuilder.InsertData(
                table: "sexos_biologicos",
                columns: new[] { "id", "ativo", "codigo", "descricao" },
                values: new object[,]
                {
                    { 1L, true, "MASCULINO", "Masculino" },
                    { 2L, true, "FEMININO", "Feminino" }
                });

            migrationBuilder.InsertData(
                table: "status_envio",
                columns: new[] { "id", "ativo", "codigo", "descricao" },
                values: new object[,]
                {
                    { 1L, true, "PENDENTE", "Notificado; aguardando ação do paciente" },
                    { 2L, true, "ATENDIDO", "Paciente realizou o registro correspondente" },
                    { 3L, true, "REAGENDADO", "Paciente pediu novo lembrete em 15 minutos" },
                    { 4L, true, "FALHOU", "Não entregue ou expirado pela janela de 2 horas (RN35)" }
                });

            migrationBuilder.InsertData(
                table: "tipos_alerta",
                columns: new[] { "id", "ativo", "codigo", "descricao" },
                values: new object[,]
                {
                    { 1L, true, "GLICEMIA", "Glicemia" },
                    { 2L, true, "MEDICAMENTO", "Medicamento" },
                    { 3L, true, "REFEICAO", "Refeição" },
                    { 4L, true, "PERSONALIZADO", "Personalizado" }
                });

            migrationBuilder.InsertData(
                table: "tipos_conteudo",
                columns: new[] { "id", "ativo", "codigo", "descricao" },
                values: new object[,]
                {
                    { 1L, true, "ARTIGO", "Artigo" },
                    { 2L, true, "DICA", "Dica" },
                    { 3L, true, "RECEITA", "Receita" },
                    { 4L, true, "VIDEO", "Vídeo" }
                });

            migrationBuilder.InsertData(
                table: "tipos_diabetes",
                columns: new[] { "id", "ativo", "codigo", "descricao" },
                values: new object[,]
                {
                    { 1L, true, "TIPO_1", "Diabetes tipo 1" },
                    { 2L, true, "TIPO_2", "Diabetes tipo 2" },
                    { 3L, true, "GESTACIONAL", "Diabetes gestacional" },
                    { 4L, true, "MODY", "MODY (Maturity Onset Diabetes of the Young)" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_alertas_paciente_id",
                table: "alertas",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_alertas_tipo_id",
                table: "alertas",
                column: "tipo_id");

            migrationBuilder.CreateIndex(
                name: "ix_alimentos_fonte_id",
                table: "alimentos",
                column: "fonte_id");

            migrationBuilder.CreateIndex(
                name: "ix_alimentos_nome",
                table: "alimentos",
                column: "nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conteudos_educativos_autor_id",
                table: "conteudos_educativos",
                column: "autor_id");

            migrationBuilder.CreateIndex(
                name: "ix_conteudos_educativos_tipo_id",
                table: "conteudos_educativos",
                column: "tipo_id");

            migrationBuilder.CreateIndex(
                name: "ix_contextos_glicemia_codigo",
                table: "contextos_glicemia",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_distribuicao_macronutrientes_plano_alimentar_id",
                table: "distribuicao_macronutrientes",
                column: "plano_alimentar_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_estados_emocionais_codigo",
                table: "estados_emocionais",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fontes_alimento_codigo",
                table: "fontes_alimento",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_formulas_energeticas_codigo",
                table: "formulas_energeticas",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_historico_alertas_alerta_id",
                table: "historico_alertas",
                column: "alerta_id");

            migrationBuilder.CreateIndex(
                name: "ix_historico_alertas_status_envio_id_data_hora_disparo",
                table: "historico_alertas",
                columns: new[] { "status_envio_id", "data_hora_disparo" });

            migrationBuilder.CreateIndex(
                name: "ix_ingredientes_receita_alimento_id",
                table: "ingredientes_receita",
                column: "alimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_ingredientes_receita_receita_id",
                table: "ingredientes_receita",
                column: "receita_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_plano_alimentar_alimento_id",
                table: "itens_plano_alimentar",
                column: "alimento_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_plano_alimentar_plano_alimentar_id",
                table: "itens_plano_alimentar",
                column: "plano_alimentar_id");

            migrationBuilder.CreateIndex(
                name: "ix_necessidades_energeticas_formula_id",
                table: "necessidades_energeticas",
                column: "formula_id");

            migrationBuilder.CreateIndex(
                name: "ix_necessidades_energeticas_nivel_atividade_id",
                table: "necessidades_energeticas",
                column: "nivel_atividade_id");

            migrationBuilder.CreateIndex(
                name: "ix_necessidades_energeticas_paciente_id",
                table: "necessidades_energeticas",
                column: "paciente_id");

            migrationBuilder.CreateIndex(
                name: "ix_niveis_atividade_codigo",
                table: "niveis_atividade",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_nutricionista_paciente_paciente_id",
                table: "nutricionista_paciente",
                column: "paciente_id");

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

            migrationBuilder.CreateIndex(
                name: "ix_perfis_usuario_codigo",
                table: "perfis_usuario",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_planos_alimentares_nutricionista_id",
                table: "planos_alimentares",
                column: "nutricionista_id");

            migrationBuilder.CreateIndex(
                name: "ix_planos_alimentares_paciente_ativo_unico",
                table: "planos_alimentares",
                column: "paciente_id",
                unique: true,
                filter: "ativo = true");

            migrationBuilder.CreateIndex(
                name: "ix_receitas_nutricionista_id",
                table: "receitas",
                column: "nutricionista_id");

            migrationBuilder.CreateIndex(
                name: "ix_registros_antropometricos_paciente_id_data_hora",
                table: "registros_antropometricos",
                columns: new[] { "paciente_id", "data_hora" });

            migrationBuilder.CreateIndex(
                name: "ix_registros_emocionais_estado_emocional_id",
                table: "registros_emocionais",
                column: "estado_emocional_id");

            migrationBuilder.CreateIndex(
                name: "ix_registros_emocionais_paciente_id_data_hora",
                table: "registros_emocionais",
                columns: new[] { "paciente_id", "data_hora" });

            migrationBuilder.CreateIndex(
                name: "ix_registros_glicemia_contexto_id",
                table: "registros_glicemia",
                column: "contexto_id");

            migrationBuilder.CreateIndex(
                name: "ix_registros_glicemia_paciente_id_data_hora",
                table: "registros_glicemia",
                columns: new[] { "paciente_id", "data_hora" });

            migrationBuilder.CreateIndex(
                name: "ix_resumo_clinico_paciente_ultima_glicemia_contexto_id",
                table: "resumo_clinico_paciente",
                column: "ultima_glicemia_contexto_id");

            migrationBuilder.CreateIndex(
                name: "ix_sexos_biologicos_codigo",
                table: "sexos_biologicos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_status_envio_codigo",
                table: "status_envio",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tipos_alerta_codigo",
                table: "tipos_alerta",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tipos_conteudo_codigo",
                table: "tipos_conteudo",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tipos_diabetes_codigo",
                table: "tipos_diabetes",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_email",
                table: "usuarios",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_perfil_id",
                table: "usuarios",
                column: "perfil_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "administradores");

            migrationBuilder.DropTable(
                name: "conteudos_educativos");

            migrationBuilder.DropTable(
                name: "distribuicao_macronutrientes");

            migrationBuilder.DropTable(
                name: "historico_alertas");

            migrationBuilder.DropTable(
                name: "ingredientes_receita");

            migrationBuilder.DropTable(
                name: "itens_plano_alimentar");

            migrationBuilder.DropTable(
                name: "necessidades_energeticas");

            migrationBuilder.DropTable(
                name: "nutricionista_paciente");

            migrationBuilder.DropTable(
                name: "registros_antropometricos");

            migrationBuilder.DropTable(
                name: "registros_emocionais");

            migrationBuilder.DropTable(
                name: "registros_glicemia");

            migrationBuilder.DropTable(
                name: "resumo_clinico_paciente");

            migrationBuilder.DropTable(
                name: "tipos_conteudo");

            migrationBuilder.DropTable(
                name: "alertas");

            migrationBuilder.DropTable(
                name: "status_envio");

            migrationBuilder.DropTable(
                name: "receitas");

            migrationBuilder.DropTable(
                name: "alimentos");

            migrationBuilder.DropTable(
                name: "planos_alimentares");

            migrationBuilder.DropTable(
                name: "formulas_energeticas");

            migrationBuilder.DropTable(
                name: "niveis_atividade");

            migrationBuilder.DropTable(
                name: "estados_emocionais");

            migrationBuilder.DropTable(
                name: "contextos_glicemia");

            migrationBuilder.DropTable(
                name: "tipos_alerta");

            migrationBuilder.DropTable(
                name: "fontes_alimento");

            migrationBuilder.DropTable(
                name: "nutricionistas");

            migrationBuilder.DropTable(
                name: "pacientes");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "sexos_biologicos");

            migrationBuilder.DropTable(
                name: "tipos_diabetes");

            migrationBuilder.DropTable(
                name: "perfis_usuario");
        }
    }
}
