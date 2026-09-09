# Gerador da documentação técnica do back-end

Produz `docs/GlicoNutri-Backend.pdf`. Ao contrário do relatório de
desenvolvimento, este documento não tem telas: ele é extraído do próprio código
e do banco, e por isso acompanha o sistema sem ficar desatualizado.

## O que é extraído automaticamente

| Fonte | O que sai |
|---|---|
| `Controllers/*.cs` | Os 81 endpoints, com verbo, rota, perfil exigido, tipo do corpo e a descrição vinda do comentário XML |
| `Services/*.cs` e `Repositories/*.cs` | Serviços, operações da interface, tamanho e as regras de negócio citadas |
| Banco de dados | Tabelas, colunas, tipos, chaves, estrangeiras, únicos, restrições de verificação e índices parciais |
| `__EFMigrationsHistory` | Migrations aplicadas |

O mapa de "qual serviço aplica qual regra" sai das citações `RN00` nos
comentários do código. Manter esses comentários é o que mantém a
rastreabilidade verificável.

## Como regerar

```bash
cd docs/gerador-backend

python3 extrair_api.py      > api.json
python3 extrair_servicos.py > servicos.json
# O schema vem do banco; veja os comandos psql no histórico ou refaça as
# consultas em information_schema para colunas, constraints e índices.

python3 gerar.py            # monta capa.html e miolo.html
node imprimir.mjs           # imprime pelo Chrome
python3 juntar.py           # une num arquivo só
```

## Onde mexer

- `texto.py` — o texto que o código não explica sozinho: arquitetura, decisões,
  fórmulas, segurança, dívida técnica
- `estilo.css` — identidade visual, compartilhada com o outro documento
- `gerar.py` — como cada seção é montada
