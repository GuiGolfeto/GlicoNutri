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
./dev.sh test        # 142 testes
```

Login inicial: `admin@gliconutri.local` / `GlicoNutri@2026`.
Recriado sozinho quando o banco está vazio, porque o sistema não permite
autocadastro (RN08 e RN09).

## Estrutura

```
backend/GlicoNutri.Api     C# / ASP.NET (.NET 10) · 16 controllers · 25 services
backend/GlicoNutri.Tests   xUnit · 142 testes
web                        Vue 3 + TypeScript · 3 páginas públicas + o sistema
db/taco                    Tabela TACO 4ª edição em CSV, 597 alimentos
db/fixtures                Amostra para testar o importador (NÃO é a tabela oficial)
docs                       Três PDFs (back-end, relatório, roteiro de gravação) e seus geradores
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

## Decisões clínicas — fechadas em 11/09/2026 pelo Mateus

1. **Equação de gasto energético.** Harris-Benedict fica na revisão de Roza e
   Shizgal (1984), mas **a escolha entre ela e Mifflin-St Jeor é do
   nutricionista**, em cada cálculo. Já era assim no código; o que mudou foi a
   descrição da fórmula, que agora diz qual revisão é.
2. **Macronutrientes pela SBD**, diabetes tipo 2: carboidrato 45–60% (padrão
   50), proteína 15–20% (padrão 20), lipídio 25–35% (padrão 30). Substitui o
   55/20/25 e as faixas mais estreitas do UC007 A2. A tela avisa quando a
   distribuição sai da faixa, mas não bloqueia — a conduta é do profissional.
   As faixas saem por `GET /api/referencias/faixas-macronutrientes`, para não
   existirem dois lugares dizendo qual é o padrão.
3. **Índice glicêmico**: a fonte padrão são as *International Tables of Glycemic
   Index and Glycemic Load Values 2021*. O alimento guarda **a origem** do
   número — tabela internacional ou informado pelo nutricionista. Os valores em
   si ainda não foram carregados.

## Divergências entre os documentos

Encontradas ao implementar. O sistema seguiu a opção indicada:

| Ponto | O que o sistema faz |
|---|---|
| Enums: DER V2 usa tipo do banco, Classes V5 usa tabela | Seguiu o V5. Índice único sobre a descrição normalizada impede "Glicose", "glicose" e "GLICOSE" conviverem |
| V4.0 e V5.0 do diagrama de classes ambas marcadas "latest" | Seguiu a V5.0 |
| Campos nos casos de uso sem coluna no DER | Seguiu o DER (confirmado em 10/11/2026). Ajustes a fazer nos UCs em `docs/AJUSTES-CASOS-DE-USO.md` |
| Tipos de diabetes: UC002 diz pré-diabetes, DER diz MODY | Os dois. Pré-diabetes entrou como opção de cadastro |
| Ator do registro antropométrico definido de 3 formas | **Só o nutricionista registra.** O paciente lê e não edita: peso e altura entram no cálculo energético e no plano |
| RF01 aceita CSV e JSON, RN10 diz só CSV | Só CSV |

## Limites conhecidos

- **Criptografia por coluna (RNF01)**: CPF, telefone, observações clínicas e
  medicação em uso são cifrados pela aplicação desde 10/11/2026 (AES-256-GCM,
  chaves em `Criptografia:Chave`/`ChaveHash` no user-secrets). O CPF também
  grava um hash determinístico (`cpf_hash`) para busca e unicidade, porque a
  cifra usa nonce aleatório e não dá pra comparar. Detalhes em
  `docs/DOCUMENTACAO-TECNICA.md` seção 4.8
- **Índice glicêmico com carga parcial**: 31 dos 597 alimentos da TACO têm o
  valor da *International Tables... 2021*, carregado em 10/11/2026 pela
  migration `IndiceGlicemicoTabelaInternacional2021`. Critério e fonte de cada
  um em `db/indice-glicemico/correspondencias.csv` — o resto não tem
  correspondência segura (arroz, feijão, macarrão, mandioca inclusive)
- **Seis alimentos sem macronutrientes**: leite líquido, sais, coco verde,
  iogurte de abacaxi — a 4ª edição traz `*` (não determinado)
- **E-mail pelo Gmail**: o sistema envia por `gliconutrismtp@gmail.com` (`smtp.gmail.com`:587,
  senha de aplicativo no user-secrets, envio confirmado em 10/11/2026). O contato público continua
  `gliconutrisuporte@gmail.com`. O Google recusa a senha comum em SMTP: precisa de **senha de
  aplicativo**, com verificação em duas etapas ativa na conta. O Resend foi
  descartado porque exige domínio próprio, e o projeto não terá um
- **Google OAuth**: ClientId configurado em 10/11/2026 (`appsettings.json` e `web/.env.development`, que é público e versionado). No Google Cloud Console, `http://localhost:5173` precisa estar entre as origens JavaScript autorizadas
- **Sem publicação** (decidido em 09/11/2026): o sistema roda só localmente. A
  RN31 (HTTPS) fica como divergência, e os links dos e-mails apontam para
  `localhost`

