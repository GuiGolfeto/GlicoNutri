# Aplicativo GlicoNutri: instalação e testes

Do zero até o aplicativo do paciente rodando no emulador Android, e o roteiro
para conferir cada função. Escrito para **Windows**, com as diferenças do macOS
indicadas onde existem.

## Como as peças se encaixam

Tudo roda no mesmo computador:

| Parte | Onde roda | Endereço |
|---|---|---|
| API | no computador | `http://localhost:5080` |
| Sistema web | no navegador do computador | `http://localhost:5173` |
| Aplicativo | no emulador Android, um celular virtual dentro do computador | enxerga a API em `http://10.0.2.2:5080` |

O `10.0.2.2` é como o emulador enxerga o próprio computador; para ele,
`localhost` é o celular virtual. O aplicativo já vem configurado com esse
endereço, então não há nada a ajustar.

**Antes de começar, a parte web precisa estar funcionando.** Siga o
`docs/INSTALACAO-WEB.md` até conseguir entrar no sistema pelo navegador. O
aplicativo usa a mesma API e o mesmo banco.

**Computador recomendado:** 16 GB de memória. Com 8 GB funciona, mas API,
sistema web, Android Studio e emulador abertos juntos deixam tudo lento.

---

## Parte 1 — Instalação

### 1.1 Ativar a virtualização

O emulador precisa da virtualização do processador ligada.

1. Abra o **Gerenciador de Tarefas** → aba **Desempenho** → **CPU**.
2. Veja a linha **Virtualização**. Se estiver **Habilitado**, pule para o 1.2.
3. Se estiver **Desabilitado**, ligue na BIOS/UEFI do computador. A opção se
   chama *Intel VT-x*, *Intel Virtualization Technology*, *AMD-V* ou *SVM*,
   conforme o fabricante. Reinicie depois.

No Windows, ligue também o recurso **Plataforma do Hipervisor do Windows**:
*Painel de Controle → Programas → Ativar ou desativar recursos do Windows*,
marque **Plataforma do Hipervisor do Windows** e reinicie.

No macOS, nada disso é necessário.

### 1.2 Instalar o Android Studio

1. Baixe em <https://developer.android.com/studio> e instale com as opções
   padrão.
2. Na primeira abertura, escolha a instalação **Standard**. Ele baixa o
   Android SDK e cria um emulador. Demora: são alguns gigabytes.
3. Ao terminar, na tela inicial, clique em **More Actions → SDK Manager**.
4. Na aba **SDK Tools**, marque **Android SDK Command-line Tools (latest)** e
   clique em **Apply**. Sem ela, o Flutter não consegue aceitar as licenças do
   Android.

### 1.3 Instalar o Flutter

1. Baixe o Flutter SDK para Windows em
   <https://docs.flutter.dev/get-started/install/windows>. A versão precisa ser
   a **3.44 ou mais nova**.
2. Extraia o zip em `C:\src\flutter`. **Não use** `C:\Program Files`: o
   Flutter precisa escrever na própria pasta, e lá ele não tem permissão.
3. Coloque `C:\src\flutter\bin` no **PATH**: menu Iniciar → *Editar as
   variáveis de ambiente do sistema* → **Variáveis de Ambiente** → em
   *Variáveis do usuário*, selecione **Path** → **Editar** → **Novo** →
   `C:\src\flutter\bin`.
4. Feche e abra o PowerShell de novo e confira:

```powershell
flutter --version
```

No macOS: `brew install --cask flutter`.

### 1.4 Aceitar as licenças e conferir o ambiente

```powershell
flutter doctor --android-licenses
```

Responda `y` a todas. Depois:

```powershell
flutter doctor
```

Precisam aparecer com ✓ as linhas **Flutter** e **Android toolchain**. As
linhas de Chrome, Visual Studio e Xcode podem ficar com aviso: não são usadas
aqui.

### 1.5 Criar o emulador

Se a instalação Standard já criou um (normalmente *Medium Phone*), pule esta
etapa.

1. No Android Studio: **More Actions → Virtual Device Manager**.
2. **Create Virtual Device** → escolha um aparelho, por exemplo **Pixel 8** →
   **Next**.
3. Escolha uma imagem de sistema recente, **com Google Play ou Google APIs**, e
   baixe se precisar → **Next** → **Finish**.
4. Clique no ▶ ao lado do emulador. Ele abre como uma janela de celular.

### 1.6 Pegar o código do aplicativo

Coloque a pasta `mobile` do aplicativo dentro da pasta do projeto, ao lado de
`backend` e `web`:

```
GlicoNutri\
  backend\
  web\
  mobile\      ← aqui
```

Depois, dentro dela:

```powershell
cd C:\caminho\do\GlicoNutri\mobile
flutter pub get
```

### 1.7 Rodar

Com a **API rodando** (terminal 1 do guia da web) e o **emulador aberto**:

```powershell
cd C:\caminho\do\GlicoNutri\mobile
flutter run
```

A **primeira vez demora** de 5 a 10 minutos: o Flutter baixa sozinho as partes
do Android que faltam e compila tudo. As próximas levam menos de um minuto.

Está no ar quando o aplicativo abre no emulador, na tela de login. Deixe esse
terminal aberto: ele é o terminal 3, ao lado da API e do sistema web. Nele,
`r` recarrega o aplicativo e `q` encerra.

Se houver mais de um aparelho conectado, o Flutter pergunta qual usar: escolha
o emulador (`emulator-5554`).

**Use sempre o `flutter run`.** Ele roda o aplicativo em modo de
desenvolvimento, que é o que aceita a API em `http://`. A versão de
distribuição exige um endereço `https://` (RN31) e se recusa a abrir sem ele.

---

## Parte 2 — Preparar um paciente para o teste

O aplicativo é **só do paciente**. Nutricionista e administrador ficam na web,
e o aplicativo recusa o login deles. Para testar, monte um paciente completo
pelo sistema web:

1. Entre na web como **nutricionista** (ou como administrador, escolhendo o
   nutricionista responsável).
2. Cadastre um paciente de teste. A **senha provisória** aparece no terminal da
   API, numa linha que começa com `[E-MAIL] Boas-vindas`. Anote.
3. No detalhe do paciente, registre:
   - **duas medições** de antropometria, para o gráfico de evolução ter dois
     pontos;
   - a **faixa glicêmica alvo** (ex.: 80 a 160);
   - o **cálculo energético**;
   - um **plano alimentar** com alimentos em algumas refeições;
   - dois **alertas** para daqui a **5 e 6 minutos**: um de **Glicemia** e um
     de **Medicamento**. O paciente já nasce com um alerta de glicemia às 8h e
     às 20h (RN24).
4. No repositório, confira se existe ao menos uma **receita** e um
   **conteúdo**.

Os dados vão para o banco real. Anote o que criou, para apagar antes da
gravação.

---

## Parte 3 — Roteiro de testes

Marque `[x]` no que funcionou. Se algo falhar, anote embaixo do passo o que
fez, o que apareceu e, se possível, tire um print (no emulador: botão de câmera
na barra lateral).

### 3.1 Login e senha *(UC001)*

- [ ] Entrar com um e-mail que não existe. **Esperado:** "E-mail ou senha
      inválidos." Essa mensagem vem da API: se aparecer, o aplicativo está
      conversando com o servidor.
- [ ] Entrar com a conta de um **nutricionista**. **Esperado:** recusado. O
      aplicativo é só do paciente (UC001 A4).
- [ ] Entrar como o paciente de teste, com a senha provisória. **Esperado:** vai
      direto para **Alterar senha** *(RN03)*.
- [ ] Tentar uma senha fraca. **Esperado:** recusada (mínimo de 8 caracteres,
      com letras e números).
- [ ] Definir a nova senha. **Esperado:** entra na tela inicial.
- [ ] Na tela de login, **Esqueci minha senha** confirma o pedido. **Esperado:**
      chega um e-mail com o link de redefinição, que abre no computador que roda o sistema.

### 3.2 Tela inicial

- [ ] Aparece "Olá" com o nome e a data de hoje.
- [ ] **Última medição** mostra o peso e o IMC registrados na web, com a
      classificação.
- [ ] Sem glicemia registrada, aparece **Registrar primeira medição**.

### 3.3 Avisos *(RN25 a RN27)*

- [ ] A tela inicial avisa **Lembretes desligados**. Tocar no aviso → **Permitir
      notificações** → **Allow**.
- [ ] Em **Mais → Lembretes**, aparecem os três alertas, com horários e dias.
      O paciente pode **Desativar**, mas não criar nem editar *(RN23)*.
- [ ] Volte para a tela inicial do **Android** (botão de início) e espere os
      horários dos alertas de teste. **Esperado:** os dois avisos aparecem no
      horário marcado, com o ícone do GlicoNutri.
- [ ] Toque no aviso de **glicemia**. **Esperado:** abre direto o registro de
      glicemia.
- [ ] *(Não registre a medição ainda: o passo 3.4 usa este aviso.)*

### 3.4 Registrar glicemia *(UC004)*

- [ ] No registro aberto pelo aviso, digite **62**, escolha **Jejum**, escolha
      uma emoção (opcional) e toque em **Salvar registro**. **Esperado:** aparece
      o alerta **Glicemia baixa**, de hipoglicemia (UC004 A2). **Leia o texto
      com atenção:** ele precisa da sua revisão clínica.
- [ ] Depois de **Entendi**, abre o **Histórico glicêmico** com a medição, a
      marcação **Hipoglicemia** e o ponto vermelho fora da faixa alvo.
- [ ] Na web, no detalhe do paciente: a glicemia aparece, e o alerta de
      glicemia consta como **ATENDIDO**. O de medicamento também aparece como
      ATENDIDO, porque lembretes que não são de glicemia avisam uma vez só.
- [ ] Registre outra medição pelo **+** da tela inicial, com um valor normal
      (ex.: 110, pós-refeição).
- [ ] Tente registrar sem escolher o momento da medição. **Esperado:**
      recusado *(RN18)*.
- [ ] Tente um valor acima de 600. **Esperado:** recusado.

### 3.5 Reenvio do lembrete *(RN26)*

- [ ] Na web, crie um alerta de **Glicemia** para daqui a 3 minutos.
- [ ] No emulador, vá para a tela inicial do Android e abra o aplicativo de
      novo: ao voltar do segundo plano, ele busca os lembretes novos. Depois,
      volte para a tela inicial do Android.
- [ ] Deixe o aviso tocar e **não registre** a glicemia. **Esperado:** o aviso
      se repete 30 minutos depois, até 4 vezes dentro de 2 horas.
- [ ] Registre a glicemia. **Esperado:** os avisos seguintes não tocam mais.

### 3.6 Histórico *(UC005)*

- [ ] Aba **Histórico**: média, mínimo, máximo e percentual no alvo batem com as
      medições feitas.
- [ ] Trocar o período (7, 15, 30 dias) muda os números.
- [ ] Tocar no gráfico mostra cada medição.

### 3.7 Plano alimentar *(UC007)*

- [ ] Aba **Plano**: aparecem o objetivo, as kcal por dia e os gramas de
      carboidrato, proteína e lipídio, iguais aos da web.
- [ ] As refeições aparecem por dia, com horários, alimentos e quantidades.
- [ ] O paciente só lê; não há como editar.

### 3.8 Diário emocional *(UC012)*

- [ ] **Mais → Diário emocional → +**. Tentar salvar sem escolher emoção.
      **Esperado:** "Selecione ao menos uma emoção." Ao escolher uma, o aviso
      some.
- [ ] Escolher uma emoção, ajustar a intensidade, escrever um comentário e
      salvar. **Esperado:** aparece na lista.
- [ ] Na web, o registro aparece para o nutricionista, e o painel emocional
      mostra a relação entre emoção e glicemia *(RF06.2)*.

### 3.9 Medidas

- [ ] **Mais → Minhas medidas**: última medição, gráfico de evolução e
      histórico.
- [ ] Não existe botão para registrar medidas. Só o nutricionista registra
      (decisão sua: evitar dado errado no cálculo do plano).

### 3.10 Conteúdos, receitas e favoritos *(RF09)*

- [ ] **Mais → Conteúdos e receitas**: aparecem os conteúdos e as receitas
      publicados.
- [ ] Buscar receita **por nome** e **por ingrediente**.
- [ ] Abrir uma receita: ingredientes, modo de preparo e valores nutricionais
      calculados a partir dos ingredientes *(RN30)*.
- [ ] Tocar no coração de uma receita e de um conteúdo. **Esperado:** aparecem
      em **Mais → Meus favoritos**.
- [ ] Na web, como administrador, **despublicar** a receita favoritada.
      **Esperado:** ela some do aplicativo, inclusive dos favoritos *(RN29)*.
      Republique depois.

### 3.11 Biometria *(RN01)*

Primeiro, cadastre uma digital no emulador:

1. No emulador, abra **Settings → Security & privacy → Device unlock** e
   defina um **PIN** (ex.: 1111).
2. Ainda ali, entre em **Fingerprint Unlock** e siga o cadastro.
3. Quando ele pedir para tocar no sensor, abra os **controles estendidos** do
   emulador (botão **⋯** na barra lateral) → **Fingerprint** → **Touch
   Sensor**. Repita até concluir o cadastro.

Depois, no aplicativo:

- [ ] Feche e abra o aplicativo de novo. **Esperado:** ele pergunta **Entrar com
      biometria?** → **Ativar** → toque no sensor pelos controles estendidos.
- [ ] Em **Mais**, a opção **Entrar com biometria** aparece ligada.
- [ ] Feche e abra o aplicativo. **Esperado:** pede a digital; ao tocar no
      sensor, entra direto na tela inicial, sem senha.
- [ ] Desligue a opção em **Mais**. Ao abrir de novo, não pede mais a digital.

### 3.12 Reinício do celular

- [ ] Com algum lembrete marcado para mais tarde, reinicie o emulador (segure o
      botão de energia na barra lateral → **Restart**).
- [ ] Desbloqueie com o PIN e **não abra o aplicativo**.
- [ ] **Esperado:** o lembrete toca no horário mesmo assim. Os avisos voltam a
      ser agendados sozinhos depois do primeiro desbloqueio.

### 3.13 Sair

- [ ] **Mais → Sair**. **Esperado:** volta para o login, e reabrir o aplicativo
      não entra sem senha.

---

## Quando algo dá errado

| O que aparece | O que é | O que fazer |
|---|---|---|
| `flutter` não é reconhecido | O PATH não foi configurado, ou o terminal não foi reaberto | Refaça o passo 1.3 e abra um PowerShell novo |
| `flutter doctor` reclama de *cmdline-tools* | Falta o pacote do passo 1.2 | SDK Manager → SDK Tools → **Android SDK Command-line Tools** |
| `Android license status unknown` | Licenças não aceitas | `flutter doctor --android-licenses` e responda `y` |
| O emulador não abre, ou fala em *HAXM*/*hypervisor* | Virtualização desligada | Passo 1.1 |
| O emulador fica muito lento | Pouca memória | Feche o que não estiver usando. No Virtual Device Manager, diminua a RAM do emulador para 2 GB |
| `No connected devices` no `flutter run` | O emulador não está aberto | Abra pelo Virtual Device Manager e rode de novo |
| Login fica carregando e dá erro de conexão | A API não está rodando | Confira o terminal 1: precisa mostrar `localhost:5080` |
| `Compile o pacote de produção com --dart-define=API_URL=https://...` | O aplicativo foi aberto em modo de distribuição | Rode com `flutter run`, sem `--release` |
| A primeira compilação passa de 15 minutos | Download das partes do Android | Espere; confira a internet. Nas próximas vezes é rápido |
| `Gradle` falha com erro de rede | Download interrompido | Rode `flutter run` de novo |
| Os avisos não aparecem | Permissão de notificação negada | Mais → Lembretes → **Permitir notificações**, ou Settings do emulador → Apps → GlicoNutri → Notifications |
| Lembrete novo da web não toca | O aplicativo só busca os lembretes quando é aberto ou volta do segundo plano | Abra o aplicativo depois de criar o alerta na web |
| A opção de biometria não aparece | Sem digital cadastrada no emulador | Passo 3.11, e feche e abra o aplicativo |
| `tenant/user not found` no terminal da API | O Supabase pausou por falta de uso | Reative o projeto no painel do Supabase |

---

## Resumo do teste

| Parte | Passou | Com problema | Observação |
|---|---|---|---|
| 3.1 Login e senha | | | |
| 3.2 Tela inicial | | | |
| 3.3 Avisos | | | |
| 3.4 Registrar glicemia | | | |
| 3.5 Reenvio do lembrete | | | |
| 3.6 Histórico | | | |
| 3.7 Plano alimentar | | | |
| 3.8 Diário emocional | | | |
| 3.9 Medidas | | | |
| 3.10 Conteúdos e favoritos | | | |
| 3.11 Biometria | | | |
| 3.12 Reinício do celular | | | |
| 3.13 Sair | | | |

Testado por: ______________________ Data: ____/____/______
