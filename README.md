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

O primeiro start cria o Administrador inicial (o sistema não permite
auto-cadastro, conforme RN08 e RN09):

```
admin@gliconutri.local / GlicoNutri@2026
```

Em desenvolvimento o envio de e-mail apenas registra no log da API — é onde
aparecem as senhas provisórias geradas nos cadastros.

## Estado

- [x] Modelo de dados completo do DER V2.0 (30 tabelas, herança TPT)
- [x] UC001 — Login e autenticação
- [x] UC002 — Cadastrar paciente
- [x] UC003 — Cadastrar nutricionista
- [ ] UC009 — Banco de alimentos e importação TACO
- [ ] UC008 — Registro antropométrico
- [ ] UC006 — Cálculo de necessidade energética
- [ ] UC007 — Elaborar plano alimentar
- [ ] UC005 / UC011 — Histórico glicêmico e dashboard
- [ ] UC010 — Alertas · RF09 — Repositório educativo
- [ ] Aplicativo mobile
