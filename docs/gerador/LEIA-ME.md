# Gerador do documento de funcionalidades

Produz `docs/GlicoNutri-Funcionalidades.pdf` a partir do sistema em execução:
as telas são capturadas do próprio Chrome, e o documento é impresso por ele.

## Como regerar

```bash
cd ~/Desktop/Projects/GlicoNutri
./dev.sh up                       # o sistema precisa estar no ar

cd docs/gerador
npm init -y && npm i puppeteer-core
pip3 install --user pypdf

node capturar.mjs                 # 16 telas, autenticando em cada perfil
python3 gerar.py                  # monta capa.html e miolo.html
node imprimir.mjs                 # imprime os dois em PDF pelo Chrome
python3 juntar.py                 # une num arquivo só
```

## Por que capa e miolo separados

O Chrome sempre reserva espaço para o rodapé quando `displayHeaderFooter` está
ligado, e isso impediria a capa de sangrar até a borda. Os dois são impressos
com configurações diferentes e unidos depois.

## Onde mexer

- `conteudo.py` — todo o texto do documento, separado do código
- `estilo.css` — identidade visual, herdada do sistema
- `capturar.mjs` — quais telas entram e em que perfil
