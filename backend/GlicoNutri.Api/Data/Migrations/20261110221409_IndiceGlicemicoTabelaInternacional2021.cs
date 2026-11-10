using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GlicoNutri.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndiceGlicemicoTabelaInternacional2021 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Decisão do Mateus em 10/11/2026: carregar o índice glicêmico das
            // International Tables of Glycemic Index and Glycemic Load Values 2021
            // (Atkinson FS, Brand-Miller JC, Foster-Powell K, Buyken AE, Goletzke J.
            // Am J Clin Nutr. 2021;114(5):1625-1632. doi:10.1093/ajcn/nqab233),
            // Supplemental Table 1 — valores testados pelo método ISO 26642:2010.
            //
            // Critério de correspondência (RN — origem do índice glicêmico): só
            // associar quando o alimento da TACO e o item da tabela internacional
            // forem claramente o mesmo alimento, no mesmo preparo (cru, cozido
            // etc.). Na dúvida, o alimento fica sem valor — o nutricionista pode
            // informar o dele por cima (origem PROFISSIONAL).
            //
            // O critério completo, item a item, está em
            // db/indice-glicemico/correspondencias.csv. Não cobre os 597 alimentos
            // da TACO: cobre os que têm correspondência inequívoca na fonte.
            //
            // IS NULL protege um valor que o nutricionista já tenha informado —
            // essa migration nunca sobrescreve origem PROFISSIONAL.
            migrationBuilder.Sql("""
                UPDATE alimentos AS a
                SET indice_glicemico = v.ig,
                    fonte_indice_glicemico_id = 1 -- Codigos.IdFonteIg.TabelaInternacional
                FROM (VALUES
                    ('Batata, inglesa, cozida', 73),
                    ('Batata, doce, cozida', 46),
                    ('Cenoura, cozida', 32),
                    ('Pão, trigo, francês', 73),
                    ('Pão, trigo, forma, integral', 75),
                    ('Leite, de vaca, integral', 37),
                    ('Leite, de vaca, desnatado, UHT', 27),
                    ('Iogurte, natural', 33),
                    ('Açúcar, refinado', 66),
                    ('Açúcar, cristal', 66),
                    ('Mel, de abelha', 60),
                    ('Banana, nanica, crua', 47),
                    ('Banana, prata, crua', 47),
                    ('Banana, maçã, crua', 47),
                    ('Banana, ouro, crua', 47),
                    ('Banana, pacova, crua', 47),
                    ('Banana, figo, crua', 47),
                    ('Maçã, Argentina, com casca, crua', 44),
                    ('Maçã, Fuji, com casca, crua', 44),
                    ('Mamão, Formosa, cru', 38),
                    ('Mamão, Papaia, cru', 38),
                    ('Manga, Haden, crua', 48),
                    ('Manga, Palmer, crua', 48),
                    ('Manga, Tommy Atkins, crua', 48),
                    ('Abacaxi, cru', 82),
                    ('Melancia, crua', 50),
                    ('Laranja, baía, suco', 48),
                    ('Laranja, da terra, suco', 48),
                    ('Laranja, lima, suco', 48),
                    ('Laranja, pêra, suco', 48),
                    ('Laranja, valência, suco', 48)
                ) AS v(nome, ig)
                WHERE a.nome = v.nome
                  AND a.fonte_id = 1 -- Codigos.Fonte.Taco
                  AND a.indice_glicemico IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Só desfaz o que esta migration gravou: alimentos da TACO cuja origem
            // ainda é a tabela internacional. Um valor que o nutricionista tenha
            // informado depois (origem PROFISSIONAL) não é tocado.
            migrationBuilder.Sql("""
                UPDATE alimentos
                SET indice_glicemico = NULL,
                    fonte_indice_glicemico_id = NULL
                WHERE fonte_id = 1
                  AND fonte_indice_glicemico_id = 1
                  AND nome IN (
                    'Batata, inglesa, cozida', 'Batata, doce, cozida', 'Cenoura, cozida',
                    'Pão, trigo, francês', 'Pão, trigo, forma, integral',
                    'Leite, de vaca, integral', 'Leite, de vaca, desnatado, UHT',
                    'Iogurte, natural', 'Açúcar, refinado', 'Açúcar, cristal',
                    'Mel, de abelha', 'Banana, nanica, crua', 'Banana, prata, crua',
                    'Banana, maçã, crua', 'Banana, ouro, crua', 'Banana, pacova, crua',
                    'Banana, figo, crua', 'Maçã, Argentina, com casca, crua',
                    'Maçã, Fuji, com casca, crua', 'Mamão, Formosa, cru',
                    'Mamão, Papaia, cru', 'Manga, Haden, crua', 'Manga, Palmer, crua',
                    'Manga, Tommy Atkins, crua', 'Abacaxi, cru', 'Melancia, crua',
                    'Laranja, baía, suco', 'Laranja, da terra, suco',
                    'Laranja, lima, suco', 'Laranja, pêra, suco', 'Laranja, valência, suco'
                  );
                """);
        }
    }
}
