# Amostra para teste do importador

`taco-amostra-teste.csv` **não é a Tabela TACO**. É um arquivo pequeno, feito
para exercitar o importador, com os casos que quebram um parser ingênuo:

| Caso | Onde aparece |
|---|---|
| Codificação Windows-1252 | o arquivo inteiro |
| Separador `;` | o arquivo inteiro |
| Decimal com vírgula | todos os valores |
| Marcador `Tr` (traço) | óleo de soja, maçã, mamão, ovo |
| Marcador `*` (não determinado) | fibras do óleo e do leite |
| Linha de seção sem valores | `FRUTAS E DERIVADOS` |
| Nome duplicado (RN11) | arroz integral, repetido no fim |
| Valor não numérico (RN10) | `Item com valor quebrado` |

Os valores nutricionais são aproximados e servem apenas ao teste. **Não use
este arquivo para popular o banco de produção** — a tabela oficial deve ser
baixada do NEPA/UNICAMP e exportada para CSV.
