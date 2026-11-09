# Roteiro de testes do GlicoNutri web

Este roteiro serve para conferir, na tela, se o sistema faz o que os documentos
do TCC pedem. Cada passo diz o que fazer, o que deve acontecer e qual regra está
sendo conferida.

**Como anotar:** marque `[x]` no que funcionou. Se algo der errado, anote embaixo
do passo o que você fez, o que apareceu na tela e, se possível, tire um print.
Quando você não concordar com algum comportamento (o sistema funcionou, mas não
deveria funcionar assim), anote do mesmo jeito: isso também é resultado.

---

## 0. Antes de começar

**Acesso.** O sistema não é publicado na internet: você o roda no seu
computador, a partir do repositório no GitHub.

Precisa ter instalados: Git, .NET 10 SDK, Node.js 20 ou mais novo e a
ferramenta do Entity Framework (`dotnet tool install --global dotnet-ef`).

```bash
git clone git@github.com:GuiGolfeto/GlicoNutri.git
cd GlicoNutri/backend/GlicoNutri.Api
dotnet user-secrets set "ConnectionStrings:Supabase" "<connection string do Supabase>"
```

A connection string fica no painel do Supabase, em **Connect**. Use a do
**pooler, na porta 5432**: a conexão direta (`db.<ref>.supabase.co`) costuma não
funcionar. Se o Supabase estiver pausado por inatividade, reative pelo painel.

No macOS ou no Linux, `./dev.sh up` (na raiz do projeto) sobe tudo. No Windows,
ou se preferir fazer à mão, use dois terminais:

```bash
# terminal 1 — API em http://localhost:5080
cd backend/GlicoNutri.Api
dotnet ef database update
dotnet run --environment Development --urls http://localhost:5080

# terminal 2 — sistema web em http://localhost:5173
cd web
npm install
npm run dev
```

Abra `http://localhost:5173` no navegador.

**Senhas provisórias.** O envio de e-mail ainda não está configurado: falta a
senha de aplicativo do Gmail (ver a seção 9). Quando você cadastrar um
nutricionista ou um paciente, **a senha provisória não chega por e-mail**: ela
aparece no terminal da API, ou em `./dev.sh logs api` se você subiu pelo script.

**Login inicial do administrador:** `admin@gliconutri.local` / `GlicoNutri@2026`

**Dados de teste.** Use nomes e e-mails inventados, mas com aparência real
(ex.: `paciente.teste1@gliconutri.local`). O CPF precisa ser válido: gere um em
qualquer gerador de CPF on-line. Os dados vão para o banco real, então anote o
que você criou para apagarmos antes da gravação.

**Ordem sugerida:** siga as seções na ordem. Cada uma usa o que foi criado na
anterior (o administrador cria o nutricionista, o nutricionista cria o
paciente, e assim por diante).

---

## 1. Páginas públicas (sem login)

- [ ] Abrir o endereço inicial. Aparece a página de divulgação, com links para
      **Funcionalidades** e **Sobre**.
- [ ] Abrir **Funcionalidades** e **Sobre**. Os textos e a ADJ estão corretos?
- [ ] Nenhum botão leva a cadastro. **Esperado:** não há "Criar conta" nem
      "Começar agora", porque o sistema não aceita autocadastro *(RN08, RN09)*.
- [ ] O botão de entrar leva à tela de login.

Observação: as imagens das páginas públicas ainda são desenhos, não capturas do
sistema.

---

## 2. Login e senha *(UC001)*

- [ ] Entrar como administrador com a senha correta. **Esperado:** abre o
      Dashboard.
- [ ] Sair. Tentar entrar com uma senha errada. **Esperado:** "E-mail ou senha
      inválidos." A mensagem não diz se o erro foi no e-mail ou na senha.
- [ ] Errar a senha **5 vezes seguidas** com um usuário de teste (não use o
      administrador). **Esperado:** a conta fica bloqueada e a tela mostra quanto
      tempo falta para liberar *(RN02)*.
      O bloqueio cresce a cada vez: 5 minutos, depois 30 minutos, depois 24 horas.
- [ ] Na tela de login, usar **Esqueci minha senha**. **Esperado:** a tela
      confirma o pedido. *(O e-mail não sai enquanto o SMTP não estiver
      configurado.)*
- [ ] O botão **Entrar com Google** só aparece quando a credencial do Google
      estiver configurada. Hoje ele não deve aparecer *(RN01)*.

---

## 3. Administrador: nutricionistas *(UC003)*

Entrar como administrador.

- [ ] O menu mostra **Nutricionistas**. *(Só o administrador vê esse item,
      RN09.)*
- [ ] Cadastrar um nutricionista com nome, e-mail e CRN. **Esperado:** aparece
      na lista, marcado como quem ainda não fez o primeiro acesso.
- [ ] Tentar cadastrar outro com o **mesmo e-mail**. **Esperado:** recusado
      *(RN06)*.
- [ ] Tentar cadastrar outro com o **mesmo CRN**. **Esperado:** recusado.
- [ ] Editar o nutricionista e salvar. Os dados mudam na lista.
- [ ] Copiar a senha provisória do log da API e guardar para a seção 4.

---

## 4. Primeiro acesso e troca de senha *(RN03)*

- [ ] Sair e entrar como o nutricionista recém-criado, com a senha provisória.
      **Esperado:** o sistema leva direto à **troca de senha**.
- [ ] Tentar abrir outra tela digitando o endereço (ex.: `/pacientes`).
      **Esperado:** volta para a troca de senha. Nada abre antes da troca.
- [ ] Tentar uma senha fraca (ex.: `12345678` ou `abcdefgh`). **Esperado:**
      recusada. A senha precisa de 8 caracteres ou mais, com letras e números.
- [ ] Trocar para uma senha válida. **Esperado:** entra no Dashboard.

---

## 5. Nutricionista: pacientes *(UC002)*

Continuar logado como o nutricionista.

- [ ] O menu **não** mostra Nutricionistas *(RN09)*. Digitar `/nutricionistas`
      no endereço também não abre a tela.
- [ ] Cadastrar um paciente: nome, e-mail, CPF, data de nascimento, sexo, tipo de
      diabetes. **Esperado:** o paciente entra na lista do nutricionista.
- [ ] Conferir as opções de tipo de diabetes. **Esperado:** aparecem pré-diabetes
      e MODY, porque cada documento cita um tipo diferente e o sistema aceitou os
      dois.
- [ ] Digitar um CPF inválido. **Esperado:** a tela avisa antes de enviar
      *(UC002 A1)*.
- [ ] Tentar outro paciente com o mesmo e-mail ou o mesmo CPF. **Esperado:**
      recusado *(UC002 A2, RN06)*.
- [ ] Na lista, o paciente novo aparece sinalizado com **faixa glicêmica
      padrão** *(RN20)*.
- [ ] Abrir o paciente e editar os dados cadastrais.
- [ ] Copiar a senha provisória do paciente do log da API (linha `[E-MAIL] Boas-vindas`) e guardar para a seção 7.

### 5.1 Faixa glicêmica alvo *(RN19, RN20)*

- [ ] No detalhe do paciente, a faixa vem vazia, e o sistema usa o padrão de 70 a
      180 mg/dL.
- [ ] Definir uma faixa própria (ex.: 80 a 150). **Esperado:** some o aviso de
      "faixa padrão". As glicemias já registradas são reclassificadas pela nova
      faixa (confira depois da seção 5.4).

### 5.2 Antropometria *(UC008)*

- [ ] Registrar peso e altura. **Esperado:** o IMC é calculado pelo sistema e
      aparece antes de salvar. Não existe campo para digitar o IMC *(RN21, UC008
      A2)*.
- [ ] Registrar uma segunda medição. **Esperado:** aparece a diferença em relação
      à anterior, em valor e em percentual *(UC008 A3)*, e o gráfico de evolução.
- [ ] Remover uma medição. **Esperado:** ela sai do histórico (no banco, é
      inativada, não apagada).

### 5.3 Necessidade energética *(UC006)*

- [ ] Com um paciente **sem** antropometria, abrir o cálculo. **Esperado:** o
      sistema bloqueia e manda registrar a antropometria primeiro *(UC006 A2)*.
- [ ] No paciente com antropometria, escolher a fórmula: **Harris-Benedict**
      (revisão de Roza e Shizgal, 1984) ou **Mifflin-St Jeor**. Escolher o fator de
      atividade.
- [ ] **Esperado:** o resultado aparece antes de confirmar e mostra TMB, fator e
      VET, não só o número final *(UC006 A4)*.
- [ ] Confira um cálculo à mão, com a fórmula escolhida. O valor bate?
- [ ] Confirmar e fazer um segundo cálculo. **Esperado:** compara com o anterior
      *(UC006 A3)*.

### 5.4 Glicemia pelo nutricionista *(UC004, UC005)*

- [ ] Registrar glicemias com contexto (jejum, pós-refeição etc.). O contexto é
      obrigatório *(RN18)*.
- [ ] Registrar um valor dentro da faixa, um abaixo e um acima. **Esperado:** os
      de fora são marcados como fora do alvo *(RN19)*.
- [ ] Tentar registrar um valor acima de 600 mg/dL. **Esperado:** recusado.
- [ ] Conferir as estatísticas do painel e o gráfico *(UC005)*. A faixa em uso
      aparece na tela.

### 5.5 Plano alimentar *(UC007)*

- [ ] Abrir **Novo plano**. **Esperado:** a distribuição já vem em 50% de
      carboidrato, 20% de proteína e 30% de lipídio, o padrão da SBD.
- [ ] Mudar os percentuais para uma soma diferente de 100%. **Esperado:** o plano
      não é confirmado *(RN15)*.
- [ ] Colocar carboidrato em 65% (fora da faixa de 45 a 60%), ajustando os outros
      para fechar 100%. **Esperado:** a tela **avisa** que está fora da faixa,
      mas **deixa salvar**: a conduta é do profissional.
- [ ] Salvar o plano. **Esperado:** mostra os gramas de cada macronutriente e o
      desvio em relação ao VET.
- [ ] Criar um segundo plano. **Esperado:** o primeiro vai para o histórico,
      inativo. Só um plano fica ativo *(RN16)*, e o histórico é preservado
      *(RN17)*.
- [ ] Usar **Duplicar** em um plano e **Reativar** um plano antigo.
      **Esperado:** reativar um plano antigo inativa o vigente.
- [ ] Em um paciente **sem** cálculo energético, tentar criar um plano.
      **Esperado:** bloqueado, porque sem VET não há plano *(RN13)*.

### 5.6 Alertas e lembretes *(UC009, RN23 a RN27)*

- [ ] No paciente recém-criado, já existe um alerta de glicemia às **8h e 20h**,
      todos os dias *(RN24)*.
- [ ] Criar um alerta de outro tipo (refeição, medicamento ou personalizado), com
      dias da semana e um ou dois horários.
- [ ] Editar e desativar um alerta.
- [ ] Usar **Verificar expirados**. Alertas pendentes há mais de 2 horas passam
      a expirados *(RN35)*.

### 5.7 Relatórios

- [ ] No detalhe do paciente, gerar os relatórios **Glicêmico**,
      **Antropométrico** e **Completo**. **Esperado:** abre um PDF.
- [ ] O relatório deixa claro se a faixa glicêmica é a padrão ou a personalizada
      *(RN20)*.
- [ ] Os dados do PDF batem com os da tela?

### 5.8 Desativar e reativar paciente *(RN05)*

- [ ] Desativar um paciente de teste. **Esperado:** ele não consegue mais entrar.
- [ ] Reativar. **Esperado:** volta a entrar, e todo o histórico continua lá.

---

## 6. Nutricionista: alimentos e repositório

### 6.1 Alimentos *(RF01, RN10 a RN12)*

- [ ] Buscar um alimento da TACO (ex.: "arroz"). **Esperado:** aparece com
      energia, proteína, lipídio, carboidrato e fibra.
- [ ] Cadastrar um alimento manualmente.
- [ ] Tentar cadastrar outro com o mesmo nome. **Esperado:** recusado *(RN11)*.
- [ ] Inativar o alimento criado. **Esperado:** some da busca, mas continua nos
      planos e receitas que já o usavam *(RN12)*.
- [ ] Importar um CSV pequeno, no formato da TACO (separador `;`, decimal com
      vírgula), com três linhas: um alimento inventado (ex.: "Teste, alimento
      um"), um alimento que já existe (ex.: "Arroz, integral, cozido") e uma
      linha com texto no lugar de número. **Esperado:** importa só o inventado,
      não duplica o que já existe *(RN11)* e mostra um **relatório das linhas
      recusadas** com o motivo *(RN10)*. Depois, inative o alimento inventado.
      *(Não use a amostra `db/fixtures/taco-amostra-teste.csv`: os valores dela
      são aproximados e entrariam no banco real.)*
- [ ] Tentar importar um arquivo que não seja CSV (ex.: JSON). **Esperado:**
      recusado. *(O RF01 fala em CSV e JSON, mas a RN10 diz só CSV, e o sistema
      seguiu a RN10.)*

### 6.2 Repositório educativo *(RF09, RN28 a RN30)*

- [ ] Cadastrar uma **receita** com ingredientes escolhidos da base de alimentos.
      **Esperado:** os valores nutricionais da receita são calculados a partir dos
      ingredientes *(RN30)*.
- [ ] Cadastrar um **conteúdo educativo** com link do YouTube e outro com um link
      comum. **Esperado:** o vídeo aparece embutido na tela do paciente; o outro
      vira link.
- [ ] Como nutricionista, **não** existe a opção de despublicar *(RN29)*.

---

## 7. Paciente na web *(área do paciente)*

Sair e entrar como o paciente criado na seção 5, com a senha provisória. Trocar
a senha no primeiro acesso, como na seção 4.

- [ ] **Esperado:** abre a **Minha área**, e não o Dashboard. Digitar
      `/pacientes` ou `/dashboard` no endereço não abre essas telas *(RN04)*.
- [ ] **Nova medição** de glicemia, com momento da medição. **Esperado:** entra no
      histórico e no gráfico dos últimos 7 dias.
- [ ] Registrar uma glicemia abaixo de 70 ou acima de 180. **Esperado:** aparece
      o alerta de hipo ou hiperglicemia. **Leia o texto do alerta com atenção:**
      ele precisa de revisão clínica sua (ver a seção 9).
- [ ] Registro emocional: **Como você está se sentindo?** Tentar salvar sem
      escolher emoção. **Esperado:** recusado, porque é preciso escolher ao menos
      uma *(RN01 do UC012)*. Escolher, definir a intensidade e salvar.
- [ ] **Seu plano alimentar** mostra o plano ativo feito na seção 5.5. O paciente
      só lê, não edita.
- [ ] **Seus lembretes** mostra os alertas configurados pelo nutricionista. O
      paciente pode desativar um alerta, mas não criar nem editar *(RN23)*.
- [ ] Peso e altura aparecem só para leitura. O paciente não registra
      antropometria (decisão sua: evitar dado errado no cálculo do plano).
- [ ] **Receitas para você** e **Para ler** mostram o repositório. Marcar uma
      receita como favorita (coração). **Esperado:** aparece em **Meus
      favoritos**.

### 7.1 De volta ao nutricionista

- [ ] Entrar como nutricionista e abrir o paciente. **Esperado:** a glicemia e o
      registro emocional feitos pelo paciente aparecem lá.
- [ ] No painel emocional, conferir a correlação entre emoção e variação
      glicêmica *(RF06.2)*.

---

## 8. Administrador: o que só ele faz

Entrar como administrador.

- [ ] Abrir **Pacientes**. **Esperado:** o administrador vê os pacientes de
      todos os nutricionistas; cada nutricionista vê só os seus *(RN33)*.
- [ ] Cadastrar um paciente. **Esperado:** o administrador precisa escolher o
      nutricionista responsável *(RN07)*.
- [ ] Tentar desativar um nutricionista que tem paciente vinculado.
      **Esperado:** recusado *(RN07)*.
- [ ] No repositório, **despublicar** a receita da seção 6.2. **Esperado:** ela
      some para o paciente, inclusive dos favoritos, e o administrador continua
      vendo para poder republicar *(RN29)*.
- [ ] Republicar a receita. Ela volta para o paciente.

### 8.1 Isolamento entre nutricionistas *(RN33)*

Este teste precisa de dois nutricionistas, cada um com um paciente.

- [ ] Logado como o nutricionista A, abrir o paciente do B pelo endereço
      (`/pacientes/<id do paciente do B>`). **Esperado:** acesso negado.
- [ ] O Dashboard do A mostra só os números dos pacientes dele.

---

## 9. Pontos que dependem de você

Durante o teste, preste atenção nestes itens. Eles foram decididos ao
implementar, porque os documentos não dizem como devem ser:

1. **Texto do alerta de hipo e hiperglicemia** (seção 7). Ele orienta seguir a
   conduta da equipe de saúde e procurar atendimento, sem prescrever tratamento.
   Está adequado clinicamente?
2. **Aviso, não bloqueio, quando o macronutriente sai da faixa da SBD**
   (seção 5.5). Está de acordo com o que você decidiu?
3. **Limites de 70 e 180 mg/dL** para hipo e hiperglicemia, valendo para todos,
   independente da faixa alvo do paciente.
4. **Os campos das telas** batem com o que você espera? Onde um caso de uso pede
   um campo que não existe no DER, o sistema seguiu o DER e o campo não existe.

O que ainda não dá para testar, porque depende de algo seu:

- **E-mail** (senha provisória, recuperação de senha): falta a senha de
  aplicativo do Gmail `gliconutrisuporte@gmail.com`. Ative a verificação em duas
  etapas e gere a senha em Conta Google → Segurança → Senhas de app. Quando
  funcionar, o link do e-mail só abre no computador que roda o sistema.
- **Entrar com Google**: falta o ClientId do Google Cloud Console.
- **Índice glicêmico dos alimentos**: a estrutura existe, mas os valores ainda
  não foram carregados.

---

## Resumo do teste

| Seção | Passou | Com problema | Observação |
|---|---|---|---|
| 1. Páginas públicas | | | |
| 2. Login e senha | | | |
| 3. Nutricionistas | | | |
| 4. Primeiro acesso | | | |
| 5. Pacientes | | | |
| 6. Alimentos e repositório | | | |
| 7. Paciente na web | | | |
| 8. Administrador | | | |

Testado por: ______________________ Data: ____/____/______
