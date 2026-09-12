# GlicoNutri — Documentação Técnica

Referência completa do sistema: arquitetura, modelo de dados, regras de negócio
implementadas, cálculos clínicos, superfície da API, ambiente web, testes e as
divergências entre o que a documentação do TCC descreve e o que o código faz.

Este documento é escrito à mão e descreve o *porquê* das decisões. O documento
irmão, `GlicoNutri-Backend.pdf`, é gerado a partir do código e do banco, e traz
o inventário exaustivo de endpoints, colunas e constraints. Quando os dois
divergirem, o gerado é o que reflete o estado atual do código.

**Emitido em 12 de setembro de 2026.**

---

## 1. O que é o sistema

Ecossistema de apoio ao controle nutricional do diabetes, construído para a
**ADJ — Associação de Diabetes Juvenil de Birigui**. É o trabalho de conclusão
de curso de Mateus Rossini Marques Pêgo, no UniSalesiano de Araçatuba, sob
orientação do professor Francisco Antônio de Sousa.

O problema que ele resolve: quem convive com diabetes mede, anota e esquece;
quem prescreve precisa desse registro na hora da consulta. O sistema liga as
duas pontas — o paciente registra glicemia e estado emocional, o nutricionista
prescreve com esse histórico à vista.

### Atores

| Ator | O que faz |
|---|---|
| **Administrador** | Cadastra nutricionistas. Herda todas as permissões do Nutricionista (RN04). Despublica conteúdo do repositório (RN29) |
| **Nutricionista** | Cadastra e acompanha pacientes vinculados a ele, registra medidas, calcula necessidade energética, elabora planos, configura alertas, publica conteúdo |
| **Paciente** | Registra glicemia e estado emocional, lê o plano vigente, o próprio histórico e o repositório educativo, marca favoritos |

**Não existe autocadastro** (RN08 e RN09). A cadeia de acesso começa num
Administrador, criado automaticamente pelo seed quando o banco está vazio e o
ambiente é de desenvolvimento.

### Dimensões

| | |
|---|---|
| API | 8.154 linhas de C#, sem contar migrations |
| Web | 7.003 linhas de Vue e TypeScript |
| Controllers | 16 classes · 87 endpoints |
| Services | 25 |
| Banco | 30 tabelas + 1 view · 8 migrations |
| Testes | 138 |
| Regras de negócio citadas no código | RN01 a RN33 e RN35 |
| Casos de uso implementados | UC001 a UC012 |

---

## 2. Arquitetura

### 2.1 Camadas

```
┌──────────────────────┐   ┌──────────────────────┐
│  Web (Vue 3 + TS)    │   │  Mobile (Flutter)    │
│  navegador           │   │  em desenvolvimento  │
└──────────┬───────────┘   └──────────┬───────────┘
           │  HTTPS + JWT Bearer      │
           └───────────┬──────────────┘
                       ▼
        ┌──────────────────────────────┐
        │  Controllers  (16)           │  formato, código HTTP, permissão
        ├──────────────────────────────┤
        │  Services     (25)           │  regra de negócio, citada por RN
        ├──────────────────────────────┤
        │  DbContext + Repositories    │  mapeamento, filtros, snake_case
        ├──────────────────────────────┤
        │  Models                      │  entidades do domínio
        └──────────────┬───────────────┘
                       ▼
            ┌─────────────────────┐
            │  PostgreSQL 17      │  Supabase (gerenciado)
            └─────────────────────┘
```

**A API é a única fronteira de segurança.** Nenhum cliente acessa o banco
diretamente. O Supabase entra apenas como PostgreSQL gerenciado: a aplicação se
conecta por Npgsql e Entity Framework, sem usar a API própria do Supabase — o
que mantém o diagrama de containers do C4 verdadeiro.

**Controllers não contêm regra.** No máximo decidem o código de resposta e
verificam, via `IAcessoPacienteService`, se quem chama pode alcançar aquele
paciente (RN33). Toda decisão clínica ou de negócio mora num Service, e cada
regra aplicada carrega o número dela em comentário — é dessa citação que o
gerador da documentação monta o mapa de rastreabilidade.

### 2.2 O caminho de uma requisição

1. **Autenticação** — o middleware JWT valida assinatura, emissor, audiência e
   validade do token, e materializa as claims.
2. **Senha provisória** — `SenhaProvisoriaMiddleware` roda entre autenticar e
   autorizar. Se a claim `senha_provisoria` for verdadeira, a única rota
   liberada é a troca de senha (RN03). Sem isso, um usuário com credencial
   ainda provisória circularia pelo sistema inteiro.
3. **Autorização** — políticas por perfil. O Administrador recebe também a role
   de Nutricionista na emissão do token, o que resolve a herança da RN04 sem
   condicional espalhada pelos controllers.
4. **Acesso ao paciente** — verificação por recurso: o Nutricionista alcança
   apenas quem está vinculado a ele; o Paciente, apenas a si mesmo.
5. **Service** — a regra.
6. **Banco** — e, quando a violação corrompe dado clínico, a mesma regra
   também como constraint.

### 2.3 Decisão: regra crítica também no banco

Regra que, se violada, corrompe dado clínico não fica só no serviço. A
unicidade de plano ativo por paciente é um índice único parcial
(`ix_planos_alimentares_paciente_ativo_unico`), e foi ele que revelou um
defeito de ordem de gravação que a aplicação escondia: o serviço inativava o
plano anterior *depois* de inserir o novo, e o banco recusou.

Hoje o schema carrega 5 verificações e 60 índices únicos.

---

## 3. Modelo de dados

### 3.1 Herança de usuários — Table-Per-Hierarchy

O DER V2.0 descreve Table-Per-Type: `usuarios` com o que é comum e uma tabela
por subtipo, cuja chave primária é também estrangeira para a base. **Esse
desenho foi vetado pelo professor orientador** e substituído por
Table-Per-Hierarchy: uma tabela `usuarios` só, com a coluna de perfil dizendo o
que cada linha é.

O discriminador é o **próprio `perfil_id`**, que já existia como chave
estrangeira obrigatória para `perfis_usuario`. Criar uma coluna separada com o
tipo diria exatamente a mesma coisa e abriria espaço para as duas divergirem.

```csharp
e.UseTphMappingStrategy();
e.HasDiscriminator(x => x.PerfilId)
 .HasValue<Paciente>(Codigos.IdPerfil.Paciente)          // 1
 .HasValue<Nutricionista>(Codigos.IdPerfil.Nutricionista) // 2
 .HasValue<Administrador>(Codigos.IdPerfil.Administrador);// 3
```

Os ids dos perfis passaram a ser constantes, porque o EF grava o valor direto
na coluna sem consultar a tabela de referência.

**O que o TPH custa.** Coluna que existia só num subtipo fica nula nas linhas
dos outros perfis, então não pode mais ser `NOT NULL`. A obrigatoriedade virou
verificação condicionada ao perfil:

```sql
ALTER TABLE usuarios ADD CONSTRAINT ck_usuarios_paciente_completo CHECK (
    perfil_id <> 1 OR (cpf IS NOT NULL AND data_nascimento IS NOT NULL
                       AND sexo_id IS NOT NULL AND tipo_diabetes_id IS NOT NULL));
ALTER TABLE usuarios ADD CONSTRAINT ck_usuarios_nutricionista_completo CHECK (
    perfil_id <> 2 OR crn IS NOT NULL);
ALTER TABLE usuarios ADD CONSTRAINT ck_usuarios_administrador_completo CHECK (
    perfil_id <> 3 OR nivel_acesso IS NOT NULL);
```

Os índices únicos de CPF e CRN continuam valendo: no PostgreSQL nulos não
colidem entre si, então a unicidade se aplica só às linhas que têm o campo.

`Telefone` existe em Paciente e Nutricionista. Sob TPH, sem mapeamento
explícito, o EF criaria `telefone` e `telefone1` para o mesmo dado — as duas
propriedades apontam para a mesma coluna.

**A migration move os dados antes de derrubar as tabelas.** A que o EF gerou
faz o contrário: `DropTable` primeiro, `AddColumn` depois — o que apagaria
todos os usuários. O `Up` foi reescrito para copiar `pacientes`,
`nutricionistas` e `administradores` para as colunas novas e só então derrubar
as origens. O `Down` faz o inverso, recriando as tabelas e devolvendo os dados.
Ambos os sentidos foram verificados contra um banco com dados.

### 3.2 O resumo clínico é uma view

O DER V2.0 previa `resumo_clinico_paciente` como tabela física, com os
indicadores do painel já calculados. **O orientador apontou violação de
Terceira Forma Normal**, e tem razão: a tabela guardava dado derivado de outras
tabelas, que envelhecia sem que nada no banco o corrigisse.

A resposta entregue ao orientador propunha resolver com *trigger*. **A
implementação foi outra, e a diferença é deliberada:** trigger continuaria
guardando o mesmo dado redundante — só automatizaria a atualização. Não corrige
a 3FN, apenas esconde o sintoma. A view elimina o armazenamento:

```sql
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
    EXISTS (...)               AS plano_ativo,
    (SELECT count(*) ...)::int AS alertas_pendentes_count,
    CASE WHEN g.data_hora IS NULL THEN NULL
         ELSE floor(EXTRACT(EPOCH FROM (now() - g.data_hora)) / 86400)::int
    END                        AS dias_sem_registro_glicemia,
    now()                      AS data_atualizacao
FROM usuarios u
LEFT JOIN LATERAL (...) g ON true   -- última medição não removida
LEFT JOIN LATERAL (...) s ON true   -- agregado dos últimos 7 dias
LEFT JOIN LATERAL (...) a ON true   -- última antropometria não removida
WHERE u.perfil_id = 1;
```

Detalhes que importam: a janela de sete dias usa `NULLIF(count(*), 0)` no
denominador, senão um paciente sem registro na semana provocaria divisão por
zero; e os `LATERAL` filtram `ativo`, para que medição removida por exclusão
lógica não conte — o mesmo critério das telas.

Consequências:

- **Não existe resumo desatualizado.** Gravar uma glicemia direto no banco, sem
  passar por nenhum Service, muda o painel na consulta seguinte. Verificado.
- **A camada de serviço perdeu 146 linhas** que existiam só para manter o
  resumo em dia. Glicemia, Antropometria, Alerta, Plano e o cadastro de
  paciente deixaram de escrever nele.
- **O `DashboardService` não mudou uma linha.** Continua lendo
  `db.ResumoClinicoPaciente`; o que mudou foi o que está do outro lado.

No EF, `ToView` sozinho não desfaz o mapeamento de tabela — é preciso
`ToTable((string?)null)` junto, senão o gerador continua emitindo migrations
para uma tabela que não existe mais.

### 3.3 Exclusão lógica

Doze entidades têm campo de situação e filtro global que as esconde quando
inativas. Dependentes sem campo próprio — histórico de alertas, distribuição de
macronutrientes, ingredientes de receita — herdam o filtro do principal, senão
sobreviveriam ao soft delete de quem os governa.

**`Alimento` fica deliberadamente fora do filtro global.** A RN12 é específica:
alimento inativado não aparece nas buscas, mas seus dados são preservados para
garantir a integridade histórica dos planos que o referenciam. Um filtro global
esconderia o alimento também quando alcançado por um item de plano, quebrando
justamente o histórico que a regra manda preservar. A exclusão é aplicada nas
consultas de busca e listagem do `AlimentoService`.

### 3.4 Tabelas de referência

O Diagrama de Classes V5.0 converteu os enums estáticos em entidades com
cadastro próprio. São 12 tabelas no formato `id + codigo + descricao + ativo`:
perfis de usuário, sexo biológico, tipo de diabetes, contexto de glicemia,
estado emocional, fórmula energética, nível de atividade, tipo de conteúdo,
tipo de alerta, status de envio, fonte de alimento e fonte de índice glicêmico.

Para que o cadastro não suje o banco, cada uma carrega **índice único sobre a
descrição normalizada**:

```sql
CREATE OR REPLACE FUNCTION normalizar_referencia(texto text)
RETURNS text LANGUAGE sql IMMUTABLE STRICT AS $$
    SELECT lower(btrim(translate(texto,
        'áàâãäéèêëíìîïóòôõöúùûüñçÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÑÇ',
        'aaaaaeeeeiiiiooooouuuuncAAAAAEEEEIIIIOOOOOUUUUNC')))
$$;
```

Assim "Glicose", "glicose" e "GLICOSE" não coexistem. A função usa `translate`
em vez de `unaccent` porque índice exige função `IMMUTABLE`, e `unaccent`
depende de um dicionário de busca que nem sempre está no `search_path` — nos
bancos descartáveis dos testes de integração, por exemplo, ele não resolve.

### 3.5 Nomenclatura

O schema físico reproduz os nomes do DER V2.0 em snake_case, inclusive onde há
dígitos (`CaloriasPor100g` → `calorias_por_100g`, `MediaGlicemia7Dias` →
`media_glicemia_7dias`). A conversão é feita por varredura do modelo depois de
toda a configuração, e preserva nomes definidos à mão.

---

## 4. Autenticação e controle de acesso — UC001

### 4.1 Credenciais

Senhas existem **apenas em hash bcrypt**, com fator de custo 12 (RN32). Texto
puro, MD5 e SHA-1 são vedados. Hash corrompido ou em formato legado é tratado
como credencial inválida em vez de derrubar a requisição.

A senha provisória gerada no cadastro usa alfabeto sem caracteres ambíguos —
sem `I`, `l`, `O`, `0`, `1` — porque na prática ela é ditada por telefone.

### 4.2 Bloqueio progressivo — RN02

Cinco tentativas inválidas consecutivas bloqueiam a conta. A espera cresce a
cada bloqueio: **5 minutos, 30 minutos e, do terceiro em diante, 24 horas**.

- Bloqueio expirado libera o acesso mas **preserva o nível atingido**, para que
  o próximo bloqueio seja mais longo.
- Autenticação bem-sucedida zera o ciclo inteiro.
- A resposta informa quantos segundos faltam, porque a tela precisa dizer isso
  ao usuário; o front mantém um cronômetro regressivo.
- O bloqueio vale para **qualquer forma de entrada**. Trocar para o login com
  Google não é atalho para escapar dele.
- Redefinir a senha encerra o bloqueio em curso — é a alternativa imediata à
  espera.
- O usuário é avisado por e-mail quando a conta é bloqueada (UC001 A5). Pode
  não ter sido ele quem tentou entrar.

### 4.3 Não vazar quem existe

Usuário inexistente, desativado (RN05) ou com senha errada recebem
**exatamente a mesma resposta**. A recuperação de senha responde `202` em todos
os casos, exista o e-mail ou não.

### 4.4 Token

JWT próprio da API, válido por 24 horas, com claims de identificação, perfil e
`senha_provisoria`. O front renova quando falta menos de uma hora e ao voltar
para a aba — sem isso a sessão morre no meio de um atendimento.

A renovação parte de uma sessão já autenticada e válida: nada é emitido para
token expirado ou conta desativada.

### 4.5 Redefinição de senha com uso único, sem tabela de tokens

O DER V2.0 não prevê tabela de tokens de recuperação. O uso único é obtido
embutindo no token uma **impressão digital do hash da senha atual**
(SHA-256 truncado em 16 caracteres). Redefinir a senha troca o hash, a
impressão deixa de bater, e qualquer link antigo ainda em circulação para de
validar sozinho.

### 4.6 Login federado — RN01

Login com Google valida o ID token contra a audiência configurada e **exige
e-mail verificado**. Não cria conta: quem não foi cadastrado por um
Administrador recebe mensagem informativa e acesso negado. Sem `ClientId`
configurado, o recurso fica desligado e a rota recusa, em vez de tentar validar
contra audiência vazia.

### 4.7 Política de senha

Mínimo de 8 caracteres, com ao menos uma letra e um número. O tamanho sozinho
aceitaria `12345678`, que é o que a regra existe para barrar.

---

## 5. Domínio clínico

### 5.1 Antropometria — UC008

IMC = peso ÷ altura², arredondado em duas casas, **sempre calculado pelo
sistema, nunca digitado** (UC008 A2). Classificação conforme o UC008 A2:

| IMC | Classificação |
|---|---|
| < 18,5 | Abaixo do peso |
| 18,5 – 24,9 | Peso normal |
| 25,0 – 29,9 | Sobrepeso |
| ≥ 30,0 | Obesidade |

A OMS ainda subdivide a obesidade em graus I, II e III; se a equipe de Nutrição
quiser esse detalhe, é estender as faixas.

Relação cintura-quadril é calculada quando as duas medidas existirem.

**Quem registra é o Nutricionista, e só ele.** O paciente lê as próprias
medidas e não as edita: peso e altura entram no cálculo energético e, por ele,
no plano alimentar — um número errado digitado pelo paciente contamina a
prescrição inteira. Decisão de 11/09/2026, que restringe o que o RF04.1
deixava em aberto.

### 5.2 Gasto energético — UC006

Duas fórmulas, **à escolha do nutricionista em cada cálculo** (RN14). A fórmula
aplicada fica gravada junto do resultado, então o histórico diz por qual
critério cada valor saiu. Código desconhecido falha alto, em vez de cair
silenciosamente numa fórmula qualquer.

**Harris-Benedict, revisão de Roza e Shizgal (1984):**

```
homens:    TMB = 88,362 + (13,397 × peso) + (4,799 × altura) − (5,677 × idade)
mulheres:  TMB = 447,593 + (9,247 × peso) + (3,098 × altura) − (4,330 × idade)
```

A equação original de 1919 usa outros coeficientes — homens 66,473 / 13,7516 /
5,0033 / 6,755; mulheres 655,0955 / 9,5634 / 1,8496 / 4,6756 — e ainda é
ensinada em parte dos cursos. A revisão de 1984 é a adotada; trocar são quatro
números.

**Mifflin-St Jeor (1990):**

```
TMB = (10 × peso) + (6,25 × altura) − (5 × idade) + (5 se homem, −161 se mulher)
```

**VET = TMB × fator de atividade**, com os fatores em tabela de referência:
sedentário 1,2 · levemente ativo 1,375 · moderadamente ativo 1,55 · muito ativo
1,725 · extremamente ativo 1,9.

### 5.3 Macronutrientes — UC007 A2

A referência clínica do sistema é a **Diretriz da Sociedade Brasileira de
Diabetes**, para diabetes tipo 2:

| Macronutriente | Faixa SBD | Padrão do sistema |
|---|---|---|
| Carboidrato | 45 – 60 % do VET | **50 %** |
| Proteína | 15 – 20 % do VET | **20 %** |
| Lipídio | 25 – 35 % do VET | **30 %** |

O padrão é o centro de cada faixa e os três somam exatamente 100 %. Substitui o
55/20/25 anterior e as faixas mais estreitas do UC007 A2 (50–60 e 25–30), que
reprovariam distribuições que a diretriz aceita.

As faixas são servidas por `GET /api/referencias/faixas-macronutrientes`, para
não existirem dois lugares dizendo qual é o padrão clínico. **Sair da faixa
avisa, não bloqueia** — a conduta é do profissional. A única trava é a RN15: a
soma tem de fechar em 100 %, com tolerância de 0,01 para ponto flutuante.

Conversão para gramas pelos fatores de Atwater: carboidrato e proteína 4 kcal/g,
lipídio 9 kcal/g.

### 5.4 Glicemia — UC004 e UC005

Cada medição entra com o contexto — jejum, pré-refeição, pós-refeição, ao
deitar ou outro — e é classificada contra a **faixa alvo daquele paciente**,
definida individualmente pelo Nutricionista (RN20). Enquanto não personalizada,
vale 70–180 mg/dL, que são também os limites clínicos de hipo e hiperglicemia.

**Mudar a faixa alvo reclassifica o histórico inteiro** (RN19). Sem isso,
sobrariam leituras antigas julgadas por um critério que não vale mais.

### 5.5 Plano alimentar — UC007

No máximo um plano ativo por paciente (RN16), garantido por índice único
parcial no banco. Ativar um novo inativa o anterior, que continua disponível no
histórico. O plano exige cálculo energético prévio (RN13).

Itens referenciam alimentos cadastrados; os valores nutricionais da base são
por 100 g e a quantidade é registrada em gramas.

### 5.6 Índice glicêmico

A TACO não publica o dado em nenhum dos 597 alimentos — e num sistema de
diabetes é a informação mais relevante clinicamente. A fonte padrão definida
para o sistema são as *International Tables of Glycemic Index and Glycemic Load
Values 2021* (Atkinson, Brand-Miller e col., *The American Journal of Clinical
Nutrition*, v. 114, n. 5, p. 1625-1632).

O alimento guarda **o valor e a origem dele**: tabela internacional ou
informado pelo nutricionista. Quem sobrescreve o número assume a autoria, e a
tela mostra isso. **Os valores ainda não foram carregados** — ver a seção 12.

---

## 6. Alertas — UC010, RN27 e RN35

O alerta é configurado pelo nutricionista, com até dois horários e os dias da
semana. Teto de dez alertas ativos simultâneos por paciente.

**Ciclo de um disparo:**

1. Nasce **pendente** na API.
2. O aplicativo busca os pendentes quando o paciente abre o app e os exibe.
3. Quando o paciente registra a glicemia correspondente — ou desativa o alerta
   — o disparo é **concluído**.
4. Passadas **2 horas** sem resposta, a varredura marca o disparo como
   **falhou** (RN35).

Toda tentativa de envio é registrada com data, status e número da tentativa
(RN27). A remoção de alerta é lógica, justamente para preservar esse histórico.

---

## 7. Repositório educativo — RF09

Conteúdos e receitas são publicados por Nutricionista ou Administrador (RN28).
A despublicação é exclusiva do Administrador e por inativação lógica (RN29).

Receita é tratada pela RN30 como especialização de conteúdo educativo, mas
modelada como tabela própria no DER V2.0, por exigir a lista de ingredientes
vinculada a alimentos cadastrados — o que permite calcular o valor nutricional
da receita a partir da base.

### 7.1 Favoritos — RF09.3

O DER V2.0 não previa tabela para favoritos, e o requisito estava registrado
como limitação. Implementado em 11/09/2026, com **duas tabelas de vínculo** —
uma por tipo de material — porque conteúdo e receita são entidades distintas e
a chave estrangeira só garante integridade apontando para uma delas.

A chave primária é o par `(paciente, material)`: marcar duas vezes não
duplica, o que deixa a tela livre para tratar o botão como interruptor sem
consultar o estado antes. Favorito é preferência, não histórico clínico —
desmarcar remove a linha de verdade.

Material despublicado some da lista de favoritos sem apagar a preferência: se
for republicado, o favorito volta sozinho.

### 7.2 Mídia por link externo — RF09.2

Imagens hospedadas exigiriam serviço de armazenamento que a arquitetura não
prevê. A opção adotada foi **link externo**: não abre frente de infraestrutura
e aproveita o material que a associação já publica. Em troca, o conteúdo
depende de terceiros e pode sair do ar.

O endereço é validado no servidor — só `http` e `https`, porque ele é
renderizado na tela do paciente e aceitar qualquer esquema abriria espaço para
`javascript:` e `data:` entrarem como "conteúdo educativo" publicado.

Vídeo do YouTube é embutido; qualquer outro endereço vira link com o domínio à
mostra. **O aviso de "não carregou" fica atrás do player**: se o iframe não
renderizar — bloqueio de rede, site fora do ar —, é o aviso que aparece no
lugar. Detectar a falha por evento não funciona: iframe e âncora disparam
`load` mesmo em página de erro. Quando o próprio YouTube carrega e mostra erro
interno, é a mensagem dele que o paciente vê.

---

## 8. Relatórios clínicos

Três documentos em PDF, gerados por QuestPDF a partir das **tabelas
transacionais**, não do modelo de leitura: o relatório precisa do histórico
completo, e não de indicadores consolidados.

| Relatório | Conteúdo |
|---|---|
| Glicêmico | Medições do período, com contexto, situação e observação |
| Antropométrico | Evolução de peso, IMC, classificação e RCQ |
| Completo | Os dois, no mesmo documento |

O cabeçalho traz a marca do projeto e o rodapé registra que o documento é de
apoio clínico e não substitui a avaliação do profissional.

`GeradorRelatorio` aparecia no Diagrama de Classes como entidade de domínio —
prática incorreta, porque gerar relatório é processo, não objeto. No código
sempre foi camada de serviço, conforme a correção do orientador.

---

## 9. Importação da tabela TACO — RF01 e RN10

A TACO é publicada em PDF e planilha, e chega ao sistema como CSV. O leitor
lida com o que a 4ª edição traz de verdade:

- **Codificação** detectada por tentativa, porque o arquivo circula em UTF-8 e
  em Latin-1.
- **Delimitador** detectado por contagem, porque a exportação varia entre
  vírgula e ponto e vírgula.
- **`*` e `Tr`** — não determinado e traço — viram nulo, e não zero. Assumir
  zero falsearia o cálculo do plano.
- **Duplicidade por nome** é recusada (RN11), com relatório de linhas
  rejeitadas e o motivo de cada uma (RN10).

Apenas CSV é aceito: o RF01 menciona CSV e JSON, mas a RN10 diz só CSV, e a
regra prevalece.

Seis alimentos da 4ª edição não têm macronutrientes — leite líquido, sais, coco
verde e iogurte de abacaxi entre eles. A tabela traz `*`, e o sistema sinaliza
a lacuna em vez de assumir zero.

---

## 10. Superfície da API

87 endpoints em 16 controllers. Convenções: `400` para violação de regra com
mensagem em português, `403` quando o perfil não alcança o recurso, `401` para
token ausente ou inválido, `423` para conta bloqueada.

| Rota base | Endpoints | Perfil |
|---|---|---|
| `api/auth` | 7 | Público, exceto troca de senha e renovação |
| `api/referencias` | 9 | Autenticado |
| `api/dashboard` | 1 | Nutricionista |
| `api/pacientes` | 7 | Nutricionista |
| `api/nutricionistas` | 6 | Administrador |
| `api/alimentos` | 7 | Nutricionista |
| `api/conteudos` | 6 | Leitura autenticada; escrita Nutricionista; despublicação Administrador |
| `api/receitas` | 6 | idem |
| `api/pacientes/{id}/glicemia` | 4 | Por paciente (RN33) |
| `api/pacientes/{id}/antropometria` | 4 | Leitura por paciente; escrita Nutricionista |
| `api/pacientes/{id}/emocoes` | 4 | Por paciente |
| `api/pacientes/{id}/necessidade-energetica` | 3 | Nutricionista |
| `api/pacientes/{id}/plano-alimentar` | 7 | Nutricionista |
| `api/pacientes/{id}/alertas` | 8 | Por paciente |
| `api/pacientes/{id}/favoritos` | 5 | Por paciente |
| `api/pacientes/{id}/relatorios` | 3 | Nutricionista |

---

## 11. Ambiente web

Vue 3 com TypeScript, Vite, Pinia e Vue Router. 19 rotas, 19 telas, 9
componentes.

### 11.1 Rotas e guardas

| Grupo | Rotas | Regra |
|---|---|---|
| Divulgação | `/`, `/funcionalidades`, `/sobre` | Abertas; não expulsam quem já está autenticado |
| Acesso | `/login`, `/recuperar-senha`, `/redefinir-senha` | Públicas; redirecionam quem já entrou |
| Troca obrigatória | `/trocar-senha` | RN03 — nenhuma outra tela abre antes |
| Paciente | `/minha-area`, `/meus-favoritos` | Perfil paciente |
| Nutricionista | `/dashboard`, `/pacientes`, `/alimentos`, `/repositorio` | RN04 |
| Administrador | `/nutricionistas` | RN09 |

A guarda espelha as políticas da API — o front não é a fronteira de segurança,
apenas evita telas que o servidor recusaria. O destino de um desvio é sempre a
tela inicial do perfil de quem está logado, nunca outra que ele também não pode
ver: mandar paciente ao dashboard criava laço de redirecionamento.

### 11.2 Sessão

Token e usuário em `localStorage`. Interceptador de requisição injeta o
`Authorization`; o de resposta derruba a sessão em `401`. O `403` não é tratado
globalmente, porque pode ser tanto falta de permissão (RN04) quanto bloqueio
por senha provisória (RN03), e cada tela reage à sua maneira.

Renovação automática a cada cinco minutos e ao voltar para a aba, quando falta
menos de uma hora para expirar.

### 11.3 Páginas públicas

Três páginas de divulgação antes do login, com moldura própria. **Nenhum botão
leva a cadastro**, porque o sistema não permite autocadastro — o caminho de
quem se interessa é o contato.

O conteúdo passou por uma poda deliberada: saíram análise preditiva de
hipoglicemia, sugestão de ajuste de insulina, contagem automática por índice e
carga glicêmica, integração hospitalar, interoperabilidade, dados em tempo
real, backup automático, conformidade com o CFM, botões das lojas e "centenas
de pacientes". Nada disso existe no sistema.

### 11.4 Sistema visual

Tema "Clinical Precision": teal como primária (`#00685d`), escala slate nos
neutros, separação por borda de 1 px em vez de sombra, Inter como tipografia
única, algarismos tabulares em todo valor clínico. A marca é uma gota com
check em `#00897B`.

---

## 12. Testes

138 testes em duas camadas.

**Unitários** (~2 s) cobrem o que é função pura: IMC e classificação,
Harris-Benedict, Mifflin-St Jeor, VET, macronutrientes, classificação
glicêmica, validador de CPF, política de senha e o leitor do CSV da TACO,
incluindo codificação, delimitador e os marcadores de valor não determinado.

**Integração** sobem a aplicação inteira contra PostgreSQL real e exercitam as
regras cuja violação corrompe dado clínico:

| Teste | O que garante |
|---|---|
| RN02 | Cinco tentativas bloqueiam, com tempo informado; bloqueios seguintes são mais longos |
| RN03 | Com senha provisória, só a troca de senha é permitida |
| RN06 | E-mail de usuário desativado continua ocupado |
| RN13 | Plano exige cálculo energético prévio |
| RN15 | Soma dos percentuais precisa fechar em 100 % |
| RN16 | Alternar planos mantém exatamente um ativo — inclusive contra escrita fora da aplicação |
| RN19 | Mudar a faixa alvo reclassifica o histórico inteiro |
| RN33 | Nutricionista não alcança paciente de outro |
| UC007 A2 | Sem ajuste, aplica 50/20/30 |
| UC008 | Paciente lê as próprias medidas, mas não registra nem remove |
| RF09.3 | Favorito não duplica e sai da lista quando o conteúdo é despublicado |
| Referências | Descrição que só difere em caixa ou acento é recusada pelo banco |

Os testes de integração **criam e destroem um banco próprio**, com prefixo
`gliconutri_teste_`, e varrem órfãos de execuções interrompidas antes de
começar. O banco da aplicação nunca é tocado — mas o fixture precisa
sobrescrever **as duas** chaves de conexão, `Supabase` e `Default`: deixar
qualquer uma apontando para o banco real faz a suíte escrever em produção.

---

## 13. Ambiente e execução

```bash
./dev.sh up          # API (:5080) e front (:5173) contra o Supabase
./dev.sh up --local  # sobe o Postgres em Docker e aponta a API para ele
./dev.sh status
./dev.sh logs api    # senhas provisórias aparecem aqui
./dev.sh down
./dev.sh test
```

Login inicial: `admin@gliconutri.local` / `GlicoNutri@2026`, recriado sozinho
quando o banco está vazio.

**Containerização.** O PostgreSQL de desenvolvimento sobe por Docker Compose,
com volume nomeado e verificação de saúde, na porta 5433 para não colidir com
um Postgres já instalado. A API também é distribuída como imagem — e é a
conteinerização que define quais hospedagens conseguem receber o sistema, o que
descarta plataformas restritas a aplicações estáticas ou a funções em
JavaScript.

**Configuração.** Nada sensível no repositório; tudo em user-secrets ou
variável de ambiente. As chaves estão documentadas em `.env.example`.

**Conexão com o Supabase.** O host de conexão direta (`db.<ref>.supabase.co`)
resolve apenas em IPv6 e nem sempre é alcançável; o acesso vai pelo pooler, que
tem IPv4, e o usuário leva o identificador do projeto (`postgres.<ref>`). A
porta 5432, em modo sessão, é **obrigatória para migrations**; a 6543, em modo
transação, é mais eficiente em produção mas exige
`No Reset On Close=true;Max Auto Prepare=0` na string.

---

## 14. Divergências entre a documentação e o sistema

Encontradas ao implementar. A decisão vigente é que **a documentação do TCC não
é atualizada**: o sistema se ajusta ao que está escrito, e as divergências
ficam registradas em vez de silenciadas.

| Ponto | O que o sistema faz |
|---|---|
| Herança TPT no DER V2.0 | **TPH**, por especificação do orientador |
| `resumo_clinico_paciente` como tabela | **View**, pelo mesmo motivo — e por view, não trigger |
| Enums: DER V2 usa tipo do banco, Classes V5 usa tabela | Seguiu o V5, com índice único sobre a descrição normalizada |
| V4.0 e V5.0 do diagrama de classes ambas marcadas "latest" | Seguiu a V5.0; corrigido na origem depois |
| Campos nos casos de uso sem coluna no DER | Seguiu o DER; os campos não existem |
| Tipos de diabetes: UC002 diz pré-diabetes, DER diz MODY | Os dois |
| Ator do registro antropométrico definido de três formas | Só o Nutricionista registra |
| RF01 aceita CSV e JSON, RN10 diz só CSV | Só CSV |
| Faixas de macronutrientes do UC007 A2 | Faixas da SBD, mais amplas |
| Nome do plano alimentar | Gravado no campo `objetivo`, que é o equivalente no DER |

---

## 15. Limites conhecidos e dívida técnica

| Item | Custo | Situação |
|---|---|---|
| **Valores de índice glicêmico** | Carga de dados | A estrutura existe — valor e origem —, mas a carga a partir das tabelas internacionais de 2021 não foi feita. Os 597 alimentos da TACO seguem sem o dado |
| **SMTP** | Credencial | A conta de envio existe. O Google recusa a senha comum da conta em SMTP: é preciso **senha de aplicativo**, com verificação em duas etapas ativa |
| **Google OAuth** | Credencial | Implementado dos dois lados; falta criar o projeto no Google Cloud Console e gerar o ClientId |
| **Publicação com HTTPS (RN31)** | Decisão | Redirecionamento e HSTS já são aplicados fora do desenvolvimento; falta decidir onde publicar. A hospedagem precisa aceitar imagem Docker |
| **Criptografia por coluna (RNF01)** | Interpretação | O Supabase criptografa em repouso no nível de disco. Criptografia por coluna quebraria busca, ordenação e as agregações do painel |
| **Campos previstos nos casos de uso** | Colunas novas | CPF, nascimento e sexo do nutricionista; endereço do paciente; observações no registro antropométrico |
| **Cadastro das tabelas de referência** | Telas novas | As tabelas aceitam cadastro e o banco já recusa duplicata, mas não há tela nem endpoint de escrita — elas só nascem por seed |
| **Cobertura de testes de serviço** | Ampliação | Repositório educativo e relatórios não têm teste automatizado próprio |
| **Modo transação no Supabase** | Ajuste de string | Usa modo sessão, exigido pelas migrations |
| **Documentação do DER** | Atualização | As tabelas de favoritos e a mudança para TPH não foram refletidas no documento definitivo |

---

## 16. Decisões em aberto

1. **View no lugar de trigger.** A resposta entregue ao orientador propunha
   trigger; a implementação foi view, pelo argumento da seção 3.2. Precisa de
   confirmação antes da defesa, porque contradiz o que já foi respondido.
2. **Campos dos casos de uso sem coluna no DER.** Cada um precisa ser revisto
   antes de assumir que a ausência é intencional — parte da documentação foi
   produzida com apoio de IA e pode ter campo esquecido nas modelagens
   seguintes.
3. **Onde publicar**, dentro dos critérios: gratuito, HTTPS, suporte a
   container, estabilidade durante a apresentação e sem período de teste que
   expire antes da banca.

---

## 17. Próximo passo

O aplicativo **Flutter** para o paciente. Os endpoints que ele consome já
existem e estão testados: registro de glicemia e de emoção, leitura das
próprias medidas, plano vigente, favoritos do repositório, ciclo de disparo de
alertas e a varredura de expiração da RN35.

A área do paciente permanece **nos dois ambientes**, web e Flutter — não é
ponte temporária até o aplicativo publicar.
