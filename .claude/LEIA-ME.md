# Contexto para sessões do Claude Code

O arquivo `CLAUDE.md` na raiz é lido automaticamente a cada sessão e traz o
essencial: como subir, convenções, decisões em aberto e limites conhecidos.

## Onde abrir o terminal

**Nesta pasta** (`~/Desktop/Projects/GlicoNutri`). A memória entre sessões é
vinculada ao diretório de trabalho, e a deste projeto está registrada aqui.

Os documentos do TCC ficam em `~/Desktop/Docs by Demand` e podem ser lidos por
caminho absoluto, sem precisar abrir a sessão lá.

## Segredos

Nada de credencial neste repositório. Tudo no user-secrets do .NET:

```bash
cd backend/GlicoNutri.Api
dotnet user-secrets list
```

As chaves esperadas estão em `.env.example`.
