using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlicoNutri.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ResumoClinicoComoView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "resumo_clinico_paciente");

            // Questão 2 da orientação — o painel deixa de ler valores guardados e
            // passa a ler cálculo. Toda coluna aqui sai das tabelas transacionais,
            // então não existe resumo desatualizado: ele nasce na hora da consulta.
            migrationBuilder.Sql("""
                CREATE VIEW resumo_clinico_paciente AS
                SELECT
                    u.id                       AS paciente_id,
                    g.valor                    AS ultima_glicemia_valor,
                    g.contexto_id              AS ultima_glicemia_contexto_id,
                    g.data_hora                AS ultima_glicemia_data,
                    s.media                    AS media_glicemia_7dias,
                    s.no_alvo                  AS percentual_no_alvo_7dias,
                    a.imc                      AS ultimo_imc,
                    a.classificacao_imc        AS ultima_classificacao_imc,
                    a.data_hora                AS ultima_data_antropometria,
                    EXISTS (SELECT 1 FROM planos_alimentares pa
                             WHERE pa.paciente_id = u.id AND pa.ativo)          AS plano_ativo,
                    (SELECT count(*)
                       FROM historico_alertas h
                       JOIN alertas al      ON al.id = h.alerta_id
                       JOIN status_envio se ON se.id = h.status_envio_id
                      WHERE al.paciente_id = u.id AND al.ativo
                        AND se.codigo = 'PENDENTE')::int                        AS alertas_pendentes_count,
                    CASE WHEN g.data_hora IS NULL THEN NULL
                         ELSE floor(EXTRACT(EPOCH FROM (now() - g.data_hora)) / 86400)::int
                    END                                                         AS dias_sem_registro_glicemia,
                    now()                                                       AS data_atualizacao
                FROM usuarios u
                -- Última medição vale a leitura mais recente que não foi removida.
                LEFT JOIN LATERAL (
                    SELECT rg.valor, rg.contexto_id, rg.data_hora
                      FROM registros_glicemia rg
                     WHERE rg.paciente_id = u.id AND rg.ativo
                     ORDER BY rg.data_hora DESC
                     LIMIT 1) g ON true
                -- Janela de 7 dias. NULLIF evita divisão por zero quando o paciente
                -- não registrou nada na semana.
                LEFT JOIN LATERAL (
                    SELECT round(avg(rg.valor)::numeric, 1)::double precision AS media,
                           round((count(*) FILTER (WHERE NOT rg.fora_do_alvo) * 100.0
                                  / NULLIF(count(*), 0))::numeric, 1)::double precision AS no_alvo
                      FROM registros_glicemia rg
                     WHERE rg.paciente_id = u.id AND rg.ativo
                       AND rg.data_hora >= now() - INTERVAL '7 days') s ON true
                LEFT JOIN LATERAL (
                    SELECT ra.imc, ra.classificacao_imc, ra.data_hora
                      FROM registros_antropometricos ra
                     WHERE ra.paciente_id = u.id AND ra.ativo
                     ORDER BY ra.data_hora DESC
                     LIMIT 1) a ON true
                WHERE u.perfil_id = 1;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW resumo_clinico_paciente;");

            migrationBuilder.CreateTable(
                name: "resumo_clinico_paciente",
                columns: table => new
                {
                    paciente_id = table.Column<long>(type: "bigint", nullable: false),
                    ultima_glicemia_contexto_id = table.Column<long>(type: "bigint", nullable: true),
                    alertas_pendentes_count = table.Column<int>(type: "integer", nullable: false),
                    data_atualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    dias_sem_registro_glicemia = table.Column<int>(type: "integer", nullable: true),
                    media_glicemia_7dias = table.Column<double>(type: "double precision", nullable: true),
                    percentual_no_alvo_7dias = table.Column<double>(type: "double precision", nullable: true),
                    plano_ativo = table.Column<bool>(type: "boolean", nullable: false),
                    ultima_classificacao_imc = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ultima_data_antropometria = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ultima_glicemia_data = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ultima_glicemia_valor = table.Column<double>(type: "double precision", nullable: true),
                    ultimo_imc = table.Column<double>(type: "double precision", nullable: true)
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
                        name: "fk_resumo_clinico_paciente_usuarios_paciente_id",
                        column: x => x.paciente_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_resumo_clinico_paciente_ultima_glicemia_contexto_id",
                table: "resumo_clinico_paciente",
                column: "ultima_glicemia_contexto_id");
            // A tabela volta vazia; repovoa com o mesmo cálculo da view, senão o
            // painel amanhece sem nenhum paciente.
            migrationBuilder.Sql("""
                INSERT INTO resumo_clinico_paciente (
                    paciente_id, ultima_glicemia_valor, ultima_glicemia_contexto_id,
                    ultima_glicemia_data, media_glicemia_7dias, percentual_no_alvo_7dias,
                    ultimo_imc, ultima_classificacao_imc, ultima_data_antropometria,
                    plano_ativo, alertas_pendentes_count, dias_sem_registro_glicemia,
                    data_atualizacao)
                SELECT
                    u.id, g.valor, g.contexto_id, g.data_hora, s.media, s.no_alvo,
                    a.imc, a.classificacao_imc, a.data_hora,
                    EXISTS (SELECT 1 FROM planos_alimentares pa
                             WHERE pa.paciente_id = u.id AND pa.ativo),
                    (SELECT count(*)
                       FROM historico_alertas h
                       JOIN alertas al      ON al.id = h.alerta_id
                       JOIN status_envio se ON se.id = h.status_envio_id
                      WHERE al.paciente_id = u.id AND al.ativo
                        AND se.codigo = 'PENDENTE')::int,
                    CASE WHEN g.data_hora IS NULL THEN NULL
                         ELSE floor(EXTRACT(EPOCH FROM (now() - g.data_hora)) / 86400)::int
                    END,
                    now()
                FROM usuarios u
                LEFT JOIN LATERAL (
                    SELECT rg.valor, rg.contexto_id, rg.data_hora
                      FROM registros_glicemia rg
                     WHERE rg.paciente_id = u.id AND rg.ativo
                     ORDER BY rg.data_hora DESC LIMIT 1) g ON true
                LEFT JOIN LATERAL (
                    SELECT round(avg(rg.valor)::numeric, 1)::double precision AS media,
                           round((count(*) FILTER (WHERE NOT rg.fora_do_alvo) * 100.0
                                  / NULLIF(count(*), 0))::numeric, 1)::double precision AS no_alvo
                      FROM registros_glicemia rg
                     WHERE rg.paciente_id = u.id AND rg.ativo
                       AND rg.data_hora >= now() - INTERVAL '7 days') s ON true
                LEFT JOIN LATERAL (
                    SELECT ra.imc, ra.classificacao_imc, ra.data_hora
                      FROM registros_antropometricos ra
                     WHERE ra.paciente_id = u.id AND ra.ativo
                     ORDER BY ra.data_hora DESC LIMIT 1) a ON true
                WHERE u.perfil_id = 1;
            """);
        }
    }
}
