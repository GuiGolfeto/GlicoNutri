# GlicoNutri

Ecossistema digital de apoio ao controle nutricional do diabetes — TCC de
Mateus Rossini Marques Pêgo, em parceria com a ADJ de Birigui.

## Stack

| Camada | Tecnologia |
|---|---|
| Back-end | C# / ASP.NET (.NET 10) + EF Core |
| Front-end web | Vue 3 + TypeScript (Vite) |
| Banco | PostgreSQL — local em Docker, Supabase em produção |
| Mobile | Flutter (etapa posterior) |

O Supabase entra apenas como Postgres gerenciado. A API continua dona da
autenticação e do controle de acesso, como descrevem o C4 e as RN01–RN04.

## Como rodar

```bash
docker compose up -d                          # Postgres na porta 5433

cd backend/GlicoNutri.Api
dotnet ef database update                     # cria o schema
ASPNETCORE_ENVIRONMENT=Development dotnet run # API em http://localhost:5080

cd ../../web
npm install && npm run dev                    # web em http://localhost:5173
```

### Apontando para o Supabase

A connection string fica no user-secrets, nunca no repositorio:

```bash
cd backend/GlicoNutri.Api
dotnet user-secrets set "ConnectionStrings:Supabase" \
  "Host=aws-0-sa-east-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<REF>;Password=<SENHA>;SSL Mode=Require;Trust Server Certificate=true"

USAR_SUPABASE=true ASPNETCORE_ENVIRONMENT=Development dotnet run
```

O host de conexao direta (`db.<REF>.supabase.co`) e **somente IPv6** e nem
sempre e alcancavel; o acesso se da pelo pooler Supavisor, que tem IPv4:

- **porta 5432 (session)** — obrigatoria para `dotnet ef database update`
- **porta 6543 (transaction)** — recomendada para a aplicacao em producao,
  exigindo `No Reset On Close=true;Max Auto Prepare=0` na string

O usuario do pooler leva o ref do projeto: `postgres.<REF>`.

O primeiro start cria o Administrador inicial (o sistema não permite
auto-cadastro, conforme RN08 e RN09):

```
admin@gliconutri.local / GlicoNutri@2026
```

Em desenvolvimento o envio de e-mail apenas registra no log da API — é onde
aparecem as senhas provisórias geradas nos cadastros. Para enviar de verdade,
preencha a seção `Email` (ver `.env.example`) no user-secrets:

```bash
dotnet user-secrets set "Email:Habilitado" "true"
dotnet user-secrets set "Email:Host" "smtp.gmail.com"
dotnet user-secrets set "Email:Usuario" "<conta>"
dotnet user-secrets set "Email:Senha" "<senha de app>"
dotnet user-secrets set "Email:Remetente" "nao-responda@adj.org.br"
```

## Estado

- [x] Modelo de dados completo do DER V2.0 (30 tabelas, herança TPT)
- [x] UC001 — Login e autenticação
- [x] UC002 — Cadastrar paciente
- [x] UC003 — Cadastrar nutricionista
- [x] UC009 — Banco de alimentos e importação TACO
- [x] UC008 — Registro antropométrico
- [x] UC006 — Cálculo de necessidade energética
- [x] UC007 — Elaborar plano alimentar
- [x] UC004 — Registrar glicemia
- [x] UC005 — Histórico glicêmico
- [x] UC011 — Dashboard do nutricionista
- [x] UC012 — Registrar emoção e correlação com glicemia
- [x] UC010 — Alertas e expiração automática
- [x] RF09 — Repositório educativo e receitas
- [x] Relatórios clínicos em PDF
- [ ] Front-end web (telas dos casos de uso clínicos)
- [ ] Aplicativo mobile
