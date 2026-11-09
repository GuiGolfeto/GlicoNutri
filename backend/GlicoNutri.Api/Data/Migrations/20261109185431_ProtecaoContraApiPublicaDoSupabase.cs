using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlicoNutri.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProtecaoContraApiPublicaDoSupabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O Supabase publica o schema public numa API REST própria, acessível
            // pelos papéis anon e authenticated. O GlicoNutri não usa essa API: a
            // nossa é a única porta (RN33), e ela conecta como dono das tabelas.
            //
            // Sem security_invoker, a view roda com as permissões de quem a criou,
            // e quem a lesse pela API do Supabase veria o resumo clínico de todos
            // os pacientes, passando por cima de qualquer RLS das tabelas de origem.
            migrationBuilder.Sql(
                "ALTER VIEW resumo_clinico_paciente SET (security_invoker = true);");

            // RLS ligado e sem política nega tudo a anon e authenticated. O dono
            // das tabelas, que é o usuário da nossa API, não é afetado: o RLS só
            // valeria para ele com FORCE ROW LEVEL SECURITY. Os papéis só existem
            // no Supabase; no Postgres local e nos bancos de teste o REVOKE é pulado.
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    t record;
                    papeis_supabase boolean := EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'anon')
                                           AND EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'authenticated');
                BEGIN
                    FOR t IN SELECT tablename FROM pg_tables WHERE schemaname = 'public' LOOP
                        EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', t.tablename);
                    END LOOP;

                    IF papeis_supabase THEN
                        REVOKE ALL ON ALL TABLES    IN SCHEMA public FROM anon, authenticated;
                        REVOKE ALL ON ALL SEQUENCES IN SCHEMA public FROM anon, authenticated;
                        REVOKE ALL ON ALL FUNCTIONS IN SCHEMA public FROM anon, authenticated;
                        -- Tabelas criadas por migrations futuras nascem sem acesso.
                        ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE ALL ON TABLES    FROM anon, authenticated;
                        ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE ALL ON SEQUENCES FROM anon, authenticated;
                        ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE ALL ON FUNCTIONS FROM anon, authenticated;
                    END IF;
                END $$;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Só desfaz a view e o RLS. Os privilégios de anon e authenticated não
            // voltam: devolvê-los reabriria os dados de saúde na API do Supabase.
            migrationBuilder.Sql(
                "ALTER VIEW resumo_clinico_paciente RESET (security_invoker);");

            migrationBuilder.Sql("""
                DO $$
                DECLARE t record;
                BEGIN
                    FOR t IN SELECT tablename FROM pg_tables WHERE schemaname = 'public' LOOP
                        EXECUTE format('ALTER TABLE public.%I DISABLE ROW LEVEL SECURITY', t.tablename);
                    END LOOP;
                END $$;
            """);
        }
    }
}
