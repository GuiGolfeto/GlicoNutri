# Tabela TACO — 4ª edição

`taco-4a-edicao.csv` traz os 597 alimentos da **Tabela Brasileira de Composição
de Alimentos (TACO), 4ª edição ampliada e revisada**, publicada pelo NEPA /
UNICAMP.

Extraído da Tabela 1 do PDF oficial (páginas 29 a 67), que traz a composição
centesimal por 100 g de parte comestível. Colunas mantidas: descrição, grupo,
energia, proteína, lipídeos, carboidrato e fibra alimentar — as que o sistema
usa. Minerais, vitaminas e colesterol ficaram de fora.

## Marcadores preservados como no original

| Marcador | Significado | Como o importador trata |
|---|---|---|
| `Tr` | traço: presente em quantidade desprezível | vira `0` |
| `*` | não determinado pelo laboratório | vira nulo, **não** zero |
| `NA` | não aplicável | vira nulo |

## O que a tabela não traz

**Índice glicêmico não existe na TACO.** Todos os alimentos importados ficam com
esse campo vazio, e ele precisa vir de outra fonte ou do julgamento do
nutricionista.

**Alguns alimentos não têm macronutrientes publicados.** O leite de vaca
líquido (integral e desnatado UHT), por exemplo, traz `*` em energia, proteína,
lipídeos e carboidrato — a 4ª edição não determinou esses valores. Um plano
alimentar que use esses itens não soma calorias por eles, e o sistema sinaliza
a lacuna em vez de assumir zero.

## Validação da extração

- 597 alimentos, 15 grupos, sem nomes duplicados
- nenhum valor fora das faixas plausíveis
- 93% dos alimentos coerentes com Atwater (4/4/9 kcal por grama) dentro de 15%;
  os desvios restantes são legítimos, e aparecem em itens como fermento
  químico, cítricos e hortaliças, onde ácidos orgânicos e polióis respondem por
  parte da massa
- 12 alimentos sorteados conferidos linha a linha contra o PDF, todos idênticos
