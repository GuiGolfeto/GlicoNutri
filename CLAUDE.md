# GlicoNutri

Ecossistema de apoio ao controle nutricional do diabetes, em parceria com a
**ADJ — Associação de Diabetes Juvenil de Birigui**. É o TCC de **Mateus
Rossini Marques Pêgo** (UniSalesiano Araçatuba, orientador Prof. Francisco
Antônio de Sousa); o dono deste repositório foi contratado por ele para
construir o sistema.

## Como subir

```bash
./dev.sh up          # API (:5080) e front (:5173) contra o Supabase
./dev.sh up --local  # usa o Postgres local em Docker
./dev.sh status
./dev.sh logs api    # senhas provisórias aparecem aqui
./dev.sh down
./dev.sh test        # 134 testes
```

Login inicial: `admin@gliconutri.local` / `GlicoNutri@2026`.
Recriado sozinho quando o banco está vazio, porque o sistema não permite
autocadastro (RN08 e RN09).

## Estrutura

```
backend/GlicoNutri.Api     C# / ASP.NET (.NET 10) · 15 controllers · 24 services
backend/GlicoNutri.Tests   xUnit · 134 testes
web                        Vue 3 + TypeScript
db/taco                    Tabela TACO 4ª edição em CSV, 597 alimentos
db/fixtures                Amostra para testar o importador (NÃO é a tabela oficial)
docs                       Dois PDFs e seus geradores
dev.sh                     Sobe, derruba e testa
```

## Banco de dados

**O banco é o Supabase**, e é o único no fluxo normal. A connection string vive
no user-secrets, nunca no repositório:

```bash
cd backend/GlicoNutri.Api
dotnet user-secrets set "ConnectionStrings:Supabase" "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;..."
```

Três coisas que economizam tempo:

- O host de conexão direta (`db.<ref>.supabase.co`) resolve **só em IPv6** e não
  é alcançável daqui. O acesso vai pelo **pooler**, e o usuário leva o ref do
  projeto: `postgres.<ref>`.
- **Porta 5432 (session)** é obrigatória para migrations. A 6543 (transaction) é
  melhor em produção, mas exige `No Reset On Close=true;Max Auto Prepare=0`.
- Os **testes de integração criam e destroem um banco próprio no Supabase**, com
  prefixo `gliconutri_teste_`, e varrem órfãos antes de começar. O banco da
  aplicação não é tocado — mas o fixture precisa sobrescrever **as duas** chaves
  de conexão, `Supabase` e `Default`: deixar qualquer uma apontando para o banco
  real faz a suíte escrever em produção. Já aconteceu uma vez.

## Convenções

- **Código e comentários em português.** Nomes de classe, método e variável
  também.
- **Comentários explicam o porquê, não o quê.** O padrão do repositório é
  registrar a razão de uma decisão não óbvia, não descrever o que a linha faz.
- **Cite a regra de negócio no ponto onde ela é aplicada** (`RN15`, `UC007 A2`).
  O gerador da documentação técnica monta o mapa de rastreabilidade a partir
  dessas citações — se elas sumirem, a documentação para de refletir o código.
- **Regras críticas também no banco**, não só no serviço. A unicidade de plano
  ativo é um índice único parcial, e foi ele que revelou um defeito de ordem de
  gravação que a aplicação escondia.
- Teste rodando, não só compilando. Vários defeitos passaram pelo compilador sem
  aviso: atributos de validação ignorados em records, claims remapeadas pelo
  .NET, ordem de gravação contra constraint.

## Documentação do projeto

Os documentos do TCC (requisitos, regras de negócio, DER, diagramas de classes e
C4, casos de uso, protótipos) estão em `~/Desktop/Docs by Demand`. Use sempre a
versão marcada **latest**.

**Decisão vigente: a documentação não é atualizada.** O sistema se ajusta ao que
está escrito, e as divergências ficam registradas em vez de resolvidas no
documento.

## Duas decisões clínicas em aberto

Nenhuma trava o sistema, mas as duas mudam o resultado das prescrições e
precisam da equipe de Nutrição:

1. **Qual Harris-Benedict.** O sistema usa a revisão de Roza e Shizgal (1984).
   A original (1919) usa outros coeficientes e ainda é ensinada. Os dois
   conjuntos estão documentados em `CalculadoraNutricional`.
2. **Distribuição padrão de macronutrientes.** Hoje 55/20/25, dentro das faixas
   do UC007. O caso de uso não fixa um valor.

## Divergências entre os documentos

Encontradas ao implementar. O sistema seguiu a opção indicada:

| Ponto | O que o sistema faz |
|---|---|
| Enums: DER V2 usa tipo do banco, Classes V5 usa tabela | Seguiu o V5, que é mais recente |
| V4.0 e V5.0 do diagrama de classes ambas marcadas "latest" | Seguiu a V5.0 |
| Campos nos casos de uso sem coluna no DER | Seguiu o DER; os campos não existem |
| Tipos de diabetes: UC002 diz pré-diabetes, DER diz MODY | Carregou o conjunto do DER |
| Ator do registro antropométrico definido de 3 formas | Decide por paciente, atendendo às três |
| RF01 aceita CSV e JSON, RN10 diz só CSV | Só CSV |

## Limites conhecidos

- **Índice glicêmico**: a TACO não publica em nenhum dos 597 alimentos
- **Seis alimentos sem macronutrientes**: leite líquido, sais, coco verde,
  iogurte de abacaxi — a 4ª edição traz `*` (não determinado)
- **Favoritos** (RF09.3) e **imagens** (RF09.2): sem lugar no DER
- **SMTP e Google OAuth**: implementados, aguardando credenciais
- **Publicação com HTTPS** (RN31): depende de decidir a hospedagem

## Próximo passo

O aplicativo **Flutter** para o paciente. Os endpoints que ele consome já
existem e estão testados: registro de glicemia, antropometria e emoções pelo
paciente, leitura do plano vigente, ciclo de disparo de alertas e a varredura
de expiração da RN35.
