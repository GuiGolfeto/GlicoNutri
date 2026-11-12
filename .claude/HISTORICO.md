# Histórico das sessões

O que foi feito, o que foi decidido e por quê. O `CLAUDE.md` da raiz diz como o
projeto funciona hoje; este arquivo diz como ele chegou aqui.

Ordem inversa: o mais recente primeiro.

---

## 9 de novembro de 2026 — documentos para o Mateus, segurança e o app no Android

### Decisões
- **Sem publicação.** O sistema roda só localmente; a RN31 (HTTPS) fica como divergência.
- **E-mail pelo Gmail** (`gliconutrisuporte@gmail.com`, senha de aplicativo). Resend descartado: exige domínio próprio, e não haverá um.
- **O Mateus roda web, API e app no mesmo PC Windows**, com o app no emulador Android pelo `flutter run` (aponta para `10.0.2.2:5080`).
- **O app só vai ao Mateus mediante pagamento** e não deve ser mencionado a ele. `mobile/`, este histórico e os trechos do app no `CLAUDE.md` e no `LEIA-ME.md` nunca sobem para o GitHub (`.git/info/exclude`; `CLAUDE.md` vai em versão recortada pelo `git update-index`). Conferido: nada do app em nenhuma branch nem commit.
- **Alarme exato** no app (`USE_EXACT_ALARM`), válido porque o app é distribuído como APK, fora da loja.

### Feito (commits `50d6006`, `467e99b`, `8f30046` no GitHub)
- `docs/ROTEIRO-DE-TESTES-WEB.md` e `docs/INSTALACAO-WEB.md`, este testado (o comando certo é `dotnet run --launch-profile http -- --urls http://localhost:5080`).
- RN33 fechada nas 9 rotas por id: 3 do paciente e 6 do nutricionista (alerta, antropometria, plano). RN03: o novo login com o token provisório é liberado. 142 testes.
- Migration `ProtecaoContraApiPublicaDoSupabase`: `security_invoker` na view, RLS em todas as tabelas, acesso de anon e authenticated revogado. Aplicada no banco real; resolve o alerta "Security Definer View".
- Mensagem ao Mateus com o que falta dele: senha de aplicativo, ClientId do Google, índice glicêmico, validações clínicas, criptografia, campos dos casos de uso, imagens das páginas públicas, roteiro de testes.

### App (só local)
- **Android compilado e testado no emulador**: todas as telas, avisos, reenvio, RN27, biometria e reagendamento depois de reiniciar. Corrigidos o aviso do diário emocional que não sumia e o texto "Mais › Segurança" do convite da biometria.
- `mobile/INSTALACAO-E-TESTES.md`: instalação no Windows e roteiro de testes do app.
- **Backup:** `mobile/` tem git próprio, com remoto `backup` em `~/Backups/GlicoNutri/mobile.git`, e um `.tar.gz` na mesma pasta. Depois de mudar o app: commit em `mobile/` e `git push backup main`.

### Pendências
1. Apagar os dados de teste antes da gravação: contas 25, 26 e 27 (a 27 tem senha `TesteAndroid2026`, plano 9 e alertas 18 e 19).
2. Receitas cadastradas sem acento ("Bolo de banana sem acucar").
3. Fechar o registro de glicemia com o "voltar" descarta o formulário sem pedir confirmação (deixado como está).
4. O que depende do Mateus, conforme a mensagem enviada a ele.

---

## 4 de novembro de 2026 — o aplicativo do paciente

Comunicação com o Mateus concluída; o que depende dele (senha de aplicativo, ClientId,
valores de IG, DER, hospedagem) ele passa depois. Construído o app Flutter em `mobile/`.
**Nada desta etapa foi commitado.**

### O que o app faz

Login com bloqueio da RN02, recuperação de senha, troca obrigatória da RN03, biometria da
RN01, início, registro de glicemia com emoção opcional, histórico com gráfico e filtros,
plano alimentar, conteúdos, receitas e favoritos, medidas (só leitura), diário emocional e
lembretes locais com o ciclo das RN25, RN26, RN27 e RN35.

**O app é só do paciente.** O usuário cogitou abri-lo a nutricionista e administrador e
desistiu: o UC001 A4, estruturado pelo Mateus, manda esses perfis para a web. Não reabrir.

### Decisões a validar com o Mateus

Os documentos não fixam estes pontos (também registrados no `CLAUDE.md`):

- Reenvio da RN26 a cada 30 minutos, quatro avisos dentro da janela de 2 horas da RN35.
- Medição até 30 minutos antes do horário atende ao lembrete.
- Só o alerta de glicemia persiste; refeição, medicamento e personalizado avisam uma vez e
  são informados como ATENDIDO, para não expirarem como falha do paciente.
- Texto do alerta de hipo e hiperglicemia (UC004 A2): precisa de revisão clínica.

### Defeitos que o app revelou no back-end

- **RN33 — um paciente alterava dados de outro pela própria rota.** O controle de acesso
  olhava só o `pacienteId` da rota, e desativar alerta, remover glicemia e remover emoção
  buscavam o registro só pelo id. Corrigido nessas três. **As rotas só de nutricionista
  têm o mesmo padrão e seguem abertas**: alerta (atualizar, reativar), antropometria
  (remover), plano (duplicar, reativar, inativar).
- **RN03 — a troca de senha prendia o usuário.** O novo login vai com o token provisório no
  cabeçalho, e o middleware respondia 403 com a senha já trocada. Os testes não pegavam
  porque usam um cliente novo, sem token. O interceptador da web envia o token do mesmo
  jeito, então a web deve ter o mesmo defeito; a correção na API vale para os dois, mas a
  web não foi testada no navegador.

### Verificação

- API: 141 de 141, rodados contra um Postgres descartável. Sem as correções, os testes
  novos de regressão falham.
- App: 41 de 41. Fluxo completo no simulador iOS contra a API real, com 29 telas capturadas;
  o teste de ponta a ponta era temporário e foi removido, porque gravaria no banco apontado.
- **Não comprovado:** notificação tocando no aparelho (o teste travou antes do fim,
  provavelmente por erro do próprio teste ao simular a saída do app), biometria e Android,
  que nunca compilou porque a máquina não tem o SDK.

### Ambiente

- A porta 5433 do `./dev.sh up --local` está ocupada por outro projeto (`bymec-test-db-1`).
- O Supabase estava pausado em 04/11 (`tenant/user not found`) e foi religado no mesmo dia,
  com os dados e as migrations intactos. Fica pausado por inatividade: conferir antes de
  gravar ou apresentar.
- `mobile/build` é link para `~/Library/Caches/gliconutri-build`: dentro do Desktop o
  `codesign` recusa o Flutter.framework. O link não vai para o repositório.
- O `Simulator.app` não foi encontrado nesta máquina; o simulador roda sem janela.

### Pendências

1. Tirar do Supabase as contas de teste antes da gravação:
   `nutri.teste.app@gliconutri.local` (id 25) e `paciente.teste.app@gliconutri.local` (id 26).
2. Corrigir as seis rotas do nutricionista da RN33; o usuário ainda não decidiu.
3. Comprovar notificação e biometria em aparelho, e compilar o Android.
4. Validar com o Mateus as decisões acima.
5. Commitar: backend, `CLAUDE.md`, `.claude/` e `mobile/` estão só no disco.

---

## 11 a 15 de setembro de 2026 — o retorno do Mateus

O Mateus devolveu o relatório de desenvolvimento num PDF de 18 páginas
(`GlicoNutri-Resposta-Desenvolvimento.pdf`), com decisões clínicas, correções de
tela, três telas de divulgação em anexo e — o mais pesado — cinco
**especificações obrigatórias do professor orientador**, duas delas contrariando
o que já estava construído.

### As duas especificações do orientador

**Herança TPT → TPH.** As tabelas `pacientes`, `nutricionistas` e
`administradores` deixaram de existir e viraram colunas de `usuarios`. O
discriminador é o próprio `perfil_id`, que já era chave estrangeira obrigatória:
criar uma segunda coluna com o tipo diria o mesmo e poderia divergir dela.

O que era `NOT NULL` num subtipo não pode mais ser, porque a coluna fica nula nas
linhas dos outros perfis — a obrigatoriedade virou CHECK condicionado ao perfil,
declarado no banco.

> A migration que o EF gerou **apagaria todos os usuários**: ela derruba as
> tabelas antes de criar as colunas. O `Up` foi reescrito à mão para copiar os
> dados primeiro, e o `Down` para devolvê-los. Os dois sentidos foram testados
> contra um banco com dados antes de encostar no real.

**Fim da `resumo_clinico_paciente`.** A tabela guardava indicadores calculados a
partir de outras tabelas — a violação de 3FN que o orientador apontou.

O Mateus respondeu ao orientador que resolveria com **trigger**. A implementação
foi **view**, e a diferença é deliberada: trigger continuaria guardando o mesmo
dado derivado, só automatizando a escrita. Não corrige a 3FN. A view elimina o
armazenamento — e, de quebra, a camada de serviço perdeu 146 linhas que existiam
apenas para manter o resumo em dia.

**Isso precisa chegar ao Mateus antes da defesa**, porque contradiz resposta que
ele já entregou.

### Decisões clínicas que ele fechou

- **Macronutrientes pela SBD**, diabetes tipo 2: 45–60 / 15–20 / 25–35, padrão no
  centro de cada faixa (50/20/30). Substitui o 55/20/25 e as faixas mais
  estreitas do UC007 A2. Fora da faixa **avisa, não bloqueia**.
- **A fórmula de gasto energético é escolha do nutricionista** — já era; o que
  mudou foi a descrição dizer qual revisão do Harris-Benedict.
- **Índice glicêmico**: fonte padrão são as International Tables 2021, e o
  alimento guarda a **origem** do valor. Os valores em si não foram carregados —
  não temos a tabela, e inventar número clínico está fora de questão.
- **Antropometria só pelo nutricionista.** Peso e altura entram no cálculo
  energético e, por ele, no plano: número errado do paciente contamina a
  prescrição inteira.
- **Pré-diabetes** entrou como opção de cadastro.
- **Enums como cadastro**, com índice único sobre a descrição normalizada.
- **Favoritos (RF09.3)** implementados em vez de marcados como limitação.
- **Mídia do conteúdo por link externo**, validado no servidor.

### Front-end

Logo correta em tudo — o que estava no sistema era um círculo meio preenchido,
que não é a marca. Três páginas públicas antes do login, com o conteúdo podado da
lista dele: saíram análise preditiva, insight de insulina, integração hospitalar,
backup automático, conformidade com o CFM, botões das lojas e "centenas de
pacientes". Login redesenhado, tela de Meus favoritos, e os quatro ajustes que
ele apontou.

Dois defeitos encontrados ao conferir as telas no navegador, não relatados por
ele: o card de necessidade energética estourava para fora da área de conteúdo — e
a causa fui eu, ao deixar a descrição da fórmula mais longa —, e o rodapé das
páginas públicas levava a documentos legais que não existem.

### Infraestrutura

- **Supabase migrado em 14/09**, com cópia em CSV das tabelas afetadas antes.
  Dados preservados, conferidos usuário a usuário.
- **Repositório publicado** em `github.com/GuiGolfeto/GlicoNutri`, privado.
- **`docs/DOCUMENTACAO-TECNICA.md`** — referência do projeto inteiro, escrita à
  mão, explicando o porquê das decisões que o PDF gerado não explica.
- **Roteiro da gravação** do vídeo de apresentação, em PDF e com gerador.

### O que ficou pendente

Nada disso é código:

1. Devolver ao Mateus a seção 4 com a coluna de situação preenchida.
2. Avisá-lo da troca de trigger por view.
3. Avisá-lo de que o Gmail exige senha de aplicativo.
4. Senha de aplicativo, ClientId do Google, valores do índice glicêmico, DER
   atualizado com favoritos e TPH, e a decisão de hospedagem.

---

## Até 8 de setembro de 2026 — a construção

Back-end e front-end web concluídos: API em .NET 10 sobre PostgreSQL no Supabase,
ambiente web em Vue 3, 597 alimentos da TACO importados, suíte de testes cobrindo
os cálculos clínicos e as regras cuja violação corrompe dado.

A área web do paciente **não estava no escopo original**. Foi criada porque o
login dele quebrava: todas as rotas exigiam perfil de nutricionista, e a guarda o
devolvia ao dashboard num laço infinito. Depois o Mateus confirmou que ela fica
nos dois ambientes, web e Flutter — não é ponte temporária.

Dois PDFs entregues, com geradores versionados que os refazem a partir do código
e do banco.
