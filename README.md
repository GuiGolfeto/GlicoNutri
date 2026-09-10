# GlicoNutri — back-end e autenticação

Ecossistema digital de apoio ao controle nutricional do diabetes — TCC de
Mateus Rossini Marques Pêgo, em parceria com a ADJ de Birigui.

Este repositório é um **recorte do projeto**: o back-end completo e, do
ambiente web, apenas o que compõe o acesso ao sistema e a tela inicial de
cada perfil. As demais telas (pacientes, banco de alimentos, repositório
educativo e gestão de nutricionistas) ficam fora daqui, embora os endpoints
que as atendem estejam presentes na API.

| Entra | Fica de fora |
|---|---|
| `backend/` inteiro — API, testes e migrations | Telas web de gestão |
| Web: login, Google, recuperação, redefinição e troca de senha | — |
| Web: Dashboard (nutricionista/admin) e Minha Área (paciente) | — |

No dashboard, o nome do paciente aparece como texto: a tela de detalhe não
faz parte do recorte.

## Stack

| Camada | Tecnologia |
|---|---|
| Back-end | C# / ASP.NET (.NET 10) + EF Core |
| Front-end web | Vue 3 + TypeScript (Vite) |
| Banco | PostgreSQL — Supabase; Docker local como alternativa |

O Supabase entra apenas como Postgres gerenciado. A API continua dona da
autenticação e do controle de acesso, como descrevem o C4 e as RN01–RN04.

## Como rodar

```bash
./dev.sh up          # sobe API (:5080) e front (:5173)
./dev.sh up --local  # usa o Postgres local em Docker
./dev.sh status
./dev.sh logs api    # as senhas provisórias aparecem aqui
./dev.sh down
./dev.sh test
```

A connection string fica no user-secrets, nunca no repositório:

```bash
cd backend/GlicoNutri.Api
dotnet user-secrets set "ConnectionStrings:Supabase" \
  "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<REF>;Password=<SENHA>;SSL Mode=Require;Trust Server Certificate=true"
```

O host de conexão direta (`db.<REF>.supabase.co`) é somente IPv6; o acesso se
dá pelo pooler, cujo usuário leva o ref do projeto (`postgres.<REF>`). A porta
5432 (session) é obrigatória para `dotnet ef database update`; a 6543
(transaction) é a recomendada em produção, exigindo
`No Reset On Close=true;Max Auto Prepare=0`.

O primeiro start cria o Administrador inicial, porque o sistema não permite
autocadastro (RN08 e RN09):

```
admin@gliconutri.local / GlicoNutri@2026
```

## Autenticação — UC001

| Rota | Regra |
|---|---|
| `POST /api/auth/login` | Credenciais em bcrypt (RN32) e bloqueio progressivo da RN02: 5 min, 30 min e 24 h |
| `POST /api/auth/login-google` | RN01 — valida o ID token; não cria conta, o e-mail precisa já existir e estar ativo |
| `POST /api/auth/renovar` | Renova o token antes das 24 h, para a sessão não cair no meio do atendimento |
| `POST /api/auth/recuperar-senha` | Responde 202 sempre, para não revelar quais e-mails existem |
| `POST /api/auth/redefinir-senha` | Uso único, garantido pela impressão digital do hash embutida no token |
| `POST /api/auth/alterar-senha` | Única rota liberada enquanto a senha ainda for a provisória (RN03) |
| `GET /api/auth/eu` | Perfil do usuário autenticado, usado pelo front para montar o menu |

Não existe cadastro público: contas de nutricionista são criadas pelo
Administrador e as de paciente pelo Nutricionista ou pelo Administrador,
sempre com senha provisória gerada pelo sistema e trocada no primeiro acesso.

### Pendências de configuração

- **SMTP** — enquanto `Email__Habilitado` for `false`, o envio apenas registra
  no log da API. É lá que aparecem a senha provisória e o link de redefinição.
- **Google OAuth** — sem `Google__ClientId` na API e `VITE_GOOGLE_CLIENT_ID` no
  front, o botão não é renderizado e a rota recusa. O login por senha segue
  sendo o caminho principal.

Ambos estão implementados; falta apenas a credencial. Ver `.env.example`.

## Testes

```bash
./dev.sh test
```

Os de integração criam um banco descartável no Supabase, com prefixo
`gliconutri_teste_`, e o destroem ao final — o banco da aplicação não é
tocado. Cobrem as regras cuja violação corrompe dado clínico: RN02, RN03,
RN06, RN13, RN15, RN16, RN19 e RN33. Recuperação de senha e login com Google
ainda não têm teste automatizado.
