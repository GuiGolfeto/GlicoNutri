# Instalação do GlicoNutri web

Do repositório no GitHub até o sistema aberto no navegador. Vale para Windows e
macOS; onde o comando muda, os dois aparecem.

O sistema tem duas partes, e as duas precisam estar rodando ao mesmo tempo:

| Parte | O que é | Endereço |
|---|---|---|
| API | C# / .NET, conversa com o banco | `http://localhost:5080` |
| Web | Vue, a tela que você usa | `http://localhost:5173` |

O banco é o Supabase do projeto. Nada de banco é instalado no seu computador.

---

## 1. Instalar as ferramentas

Instale uma vez só, nesta ordem:

| Ferramenta | Versão | Onde baixar |
|---|---|---|
| Git | qualquer recente | <https://git-scm.com/downloads> |
| .NET SDK | **10** (o SDK, não só o Runtime) | <https://dotnet.microsoft.com/download/dotnet/10.0> |
| Node.js | **22 LTS** (20.19 ou mais novo também serve) | <https://nodejs.org> |

No Windows, aceite as opções padrão dos instaladores.

**Feche e abra o terminal de novo** depois de instalar. Sem isso, o terminal
não enxerga as ferramentas novas. No Windows, use o **PowerShell**; no macOS, o
**Terminal**.

Confira se as três estão instaladas:

```bash
git --version
dotnet --version     # precisa começar com 10.
node --version       # precisa ser v22.x (ou v20.19 em diante)
```

Instale a ferramenta de banco do .NET:

```bash
dotnet tool install --global dotnet-ef
```

Feche e abra o terminal mais uma vez e confira:

```bash
dotnet ef --version
```

---

## 2. Baixar o projeto

O repositório é privado. Antes, aceite o convite que chegou no seu e-mail do
GitHub.

Escolha uma pasta (a Área de Trabalho, por exemplo) e rode:

```bash
cd Desktop
git clone https://github.com/GuiGolfeto/GlicoNutri.git
cd GlicoNutri
```

Na primeira vez, o Git abre o navegador para você entrar na sua conta do
GitHub. É só autorizar.

---

## 3. Ligar a API ao banco

A API precisa da connection string do Supabase. Ela não vem no repositório,
porque tem a senha do banco: fica guardada só no seu computador, no
*user-secrets* do .NET.

### 3.1 Montar a connection string

No painel do Supabase, abra o projeto e clique em **Connect**. Na lista, use o
**Session pooler**: é ele que funciona daqui. A conexão direta
(`db.<ref>.supabase.co`) não serve.

Pegue de lá o **host** e o **usuário**, e monte neste formato, tudo numa linha:

```
Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.SEU_REF;Password=SUA_SENHA;SSL Mode=Require;Trust Server Certificate=true
```

- **Host:** confira se é o mesmo do painel. O começo (`aws-0-sa-east-1`) muda
  conforme a região.
- **Username:** é `postgres.` seguido do código do projeto. O ponto faz parte.
- **Port:** 5432. A 6543 não funciona para o passo 4.
- **Password:** é a senha do **banco**, não a da sua conta do Supabase. Se não
  lembrar, gere outra em *Project Settings → Database → Reset database password*.
  Se você trocar essa senha, avise o desenvolvedor, porque a dele para de
  funcionar.

### 3.2 Guardar a connection string

```bash
cd backend/GlicoNutri.Api
dotnet user-secrets set "ConnectionStrings:Supabase" "COLE_AQUI_A_CONNECTION_STRING"
```

Mantenha as aspas. Se a senha tiver `;` ou `"`, troque por uma sem esses
caracteres: eles quebram a connection string.

### 3.3 Chaves de criptografia e e-mail

O CPF, o telefone e as observações clínicas ficam cifrados no banco (RNF01).
Sem as chaves a API nem sobe, e elas precisam ser **as mesmas** usadas pelo
desenvolvedor: com outras, os dados já gravados não podem ser lidos. Peça os
valores a ele por um canal privado — nunca por e-mail aberto, nunca no repositório.

```bash
dotnet user-secrets set "Criptografia:Chave"     "VALOR_RECEBIDO"
dotnet user-secrets set "Criptografia:ChaveHash" "VALOR_RECEBIDO"

dotnet user-secrets set "Email:Habilitado" "true"
dotnet user-secrets set "Email:Host"       "smtp.gmail.com"
dotnet user-secrets set "Email:Porta"      "587"
dotnet user-secrets set "Email:Usuario"    "gliconutrismtp@gmail.com"
dotnet user-secrets set "Email:Remetente"  "gliconutrismtp@gmail.com"
dotnet user-secrets set "Email:Senha"      "SENHA_DE_APLICATIVO"
```

Sem o bloco de e-mail o sistema funciona, mas as senhas provisórias só
aparecem no log da API.

---

## 4. Atualizar o banco

Ainda na pasta `backend/GlicoNutri.Api`:

```bash
dotnet ef database update
```

A primeira execução demora, porque compila o projeto. No fim deve aparecer
**Done.** Se o banco já estiver atualizado, também aparece **Done.**, e nada é
alterado.

---

## 5. Subir a API (terminal 1)

Ainda na pasta `backend/GlicoNutri.Api`:

```bash
dotnet run --launch-profile http -- --urls http://localhost:5080
```

Está no ar quando aparecerem estas duas linhas:

```
Now listening on: http://localhost:5080
Hosting environment: Development
```

**Deixe este terminal aberto.** Fechar o terminal derruba a API. É também nele
que aparecem as senhas provisórias de quem você cadastrar, numa linha que começa
com `[E-MAIL] Boas-vindas`.

O endereço precisa ser exatamente `5080`: é nele que a tela procura a API.

---

## 6. Subir a tela (terminal 2)

Abra **outro** terminal e vá até a pasta do projeto:

```bash
cd Desktop/GlicoNutri/web
npm install
npm run dev
```

O `npm install` só é necessário na primeira vez e depois de cada atualização do
projeto. Está no ar quando aparecer:

```
➜  Local:   http://localhost:5173/
```

Deixe este terminal aberto também.

---

## 7. Abrir o sistema

No navegador, abra **<http://localhost:5173>**.

Escreva `localhost`, e não `127.0.0.1`: a API só aceita chamadas vindas de
`localhost:5173`, e pelo outro endereço o login falha sem explicar por quê.

Login inicial do administrador:

```
admin@gliconutri.local
GlicoNutri@2026
```

Daqui em diante, siga o `docs/ROTEIRO-DE-TESTES-WEB.md`.

---

## Atalho para macOS

No macOS, depois dos passos 1 a 3, um comando na raiz do projeto faz os passos 4
a 6 e roda tudo em segundo plano:

```bash
./dev.sh up        # sobe
./dev.sh status    # mostra o que está rodando
./dev.sh logs api  # mostra o log da API (senhas provisórias)
./dev.sh down      # derruba
```

No Windows, o `dev.sh` não funciona: use os passos 4 a 6.

---

## No dia a dia

**Para usar de novo:** repita só os passos 5 e 6, cada um no seu terminal.

**Para parar:** `Ctrl + C` em cada terminal.

**Para pegar uma versão nova do projeto:**

```bash
cd Desktop/GlicoNutri
git pull
```

Depois rode o passo 4 (atualizar o banco), o `npm install` do passo 6 e suba de
novo.

---

## Quando algo dá errado

| O que aparece | O que é | O que fazer |
|---|---|---|
| `tenant/user ... not found` | O Supabase pausou por falta de uso | Reative o projeto no painel do Supabase e espere um ou dois minutos. A credencial está certa |
| `Connection string 'Supabase' não configurada` | O passo 3.2 não foi feito, ou foi feito em outra pasta | Rode o `user-secrets set` dentro de `backend/GlicoNutri.Api` |
| `CriptografiaOptions` ... `Chave` / `ChaveHash` | O passo 3.3 não foi feito | Configure as duas chaves com os valores recebidos do desenvolvedor |
| `The computed authentication tag did not match` | As chaves são diferentes das usadas para cifrar | Confira se copiou as duas chaves exatamente como recebidas |
| `password authentication failed` | Senha ou usuário errado na connection string | Confira o `postgres.SEU_REF` e a senha do banco, e rode o 3.2 de novo |
| `No such host is known` / `Name or service not known` | Host errado, ou a conexão direta em vez do pooler | Use o host do **Session pooler** |
| Fica parado conectando e dá timeout | A rede bloqueia a porta 5432 (comum em redes de faculdade e empresa) | Teste em outra rede, como o roteador do celular |
| `dotnet ef` não é reconhecido | O terminal não foi reaberto depois do `tool install` | Feche e abra o terminal. Se persistir no macOS: `export PATH="$PATH:$HOME/.dotnet/tools"` |
| `address already in use` / porta em uso | Já tem uma API ou tela rodando | Feche o outro terminal, ou `Ctrl + C` nele |
| A tela abre, mas o login não faz nada ou dá erro de rede | A API não está rodando, ou está em outra porta | Confira o terminal 1: precisa mostrar `localhost:5080` |
| `E-mail ou senha inválidos.` com o administrador | A senha do administrador já foi trocada nesse banco | Peça a senha atual ao desenvolvedor |
| Erro de versão no `npm install` ou no `npm run dev` | Node antigo | Instale o Node 22 LTS e rode `npm install` de novo |

---

## Aplicativo do paciente

A API configurada aqui é a mesma que o aplicativo usa. Para instalar o Android
Studio, o Flutter e rodar o app no emulador, siga `mobile/INSTALACAO-E-TESTES.md`.
