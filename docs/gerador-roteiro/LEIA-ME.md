# Gerador do roteiro de gravação

Produz `docs/GlicoNutri-Roteiro-de-Gravacao.pdf` — o roteiro que o Mateus segue
para gravar a apresentação do sistema.

| Arquivo | O que é |
|---|---|
| `roteiro.html` | A fonte. É um fragmento, sem `<html>` e `<body>`, porque é também o que sobe como página publicada |
| `montar.py` | Envolve o fragmento num documento completo, fixa o tema claro e acrescenta as regras de impressão |
| `imprimir.mjs` | Imprime pelo Chrome, em A4, com rodapé numerado |

## Como regerar

```bash
cd docs/gerador-roteiro
npm install --no-save puppeteer-core   # só na primeira vez
python3 montar.py
node imprimir.mjs
```

O `roteiro-impressao.html` é intermediário e não vai para o repositório.

## Ao editar

Mexa só no `roteiro.html`. Três coisas que a versão impressa depende e que é
fácil quebrar sem perceber:

- **Os blocos `<details>` viram abertos na impressão.** No papel, fechados,
  esconderiam as respostas das perguntas de banca.
- **Cada `.cena` cabe inteira numa página.** Se uma cena crescer demais, ela
  passa a ocupar a página sozinha — vale dividir em duas.
- **As cores são impressas.** Tarja de cena, faixa de fala e caixas de alerta
  carregam informação; em cinza elas perdem o sentido.

## Os tempos

A linha do tempo no topo do documento e os timecodes de cada cena precisam
bater. Se você mudar a duração de uma cena, ajuste as duas coisas — não há nada
calculando isso automaticamente.
