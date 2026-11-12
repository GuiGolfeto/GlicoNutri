# Contexto para sessões do Claude Code

O arquivo `CLAUDE.md` na raiz é lido automaticamente a cada sessão e traz o
essencial: como subir, convenções, decisões em aberto e limites conhecidos.

`HISTORICO.md`, nesta pasta, registra o caminho: o que foi feito em cada etapa,
o que foi decidido e por quê. O `CLAUDE.md` diz como o projeto é hoje; o
histórico diz como ele chegou aqui.

## Onde abrir o terminal

**Nesta pasta** (`~/Desktop/Projects/GlicoNutri`). A memória entre sessões é
vinculada ao diretório de trabalho, e a deste projeto está registrada aqui.

Os documentos do TCC ficam em `~/Desktop/Docs by Demand` e podem ser lidos por
caminho absoluto, sem precisar abrir a sessão lá.

## Aplicativo

O app do paciente está em `mobile/`, e o `mobile/README.md` explica como rodar e as
armadilhas de ambiente. Antes de compilar em outra máquina dentro do Desktop, recrie o link
de `mobile/build`, que não vai para o repositório.

## Segredos

Nada de credencial neste repositório. Tudo no user-secrets do .NET:

```bash
cd backend/GlicoNutri.Api
dotnet user-secrets list
```

As chaves esperadas estão em `.env.example`.
