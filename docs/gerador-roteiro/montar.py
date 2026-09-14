# -*- coding: utf-8 -*-
"""Envolve o roteiro num documento completo e acrescenta o CSS de impressao.

O roteiro.html e um fragmento: sem <html>, <head> ou <body>, porque e o formato
que a publicacao como pagina espera. Para imprimir ele precisa virar documento
inteiro, com o tema claro fixado e as regras de quebra de pagina.
"""
import pathlib

AQUI = pathlib.Path(__file__).parent
html = (AQUI / "roteiro.html").read_text()

# No papel o acordeao precisa nascer aberto — senao metade do conteudo some.
html = html.replace("<details>", "<details open>")

IMPRESSAO = """
<style media="print">
  @page { size: A4; margin: 16mm 15mm 14mm; }

  html, body { background: #fff !important; }
  body { font-size: 10.5pt; line-height: 1.5; }
  .folha { max-width: none; padding: 0; }

  /* Cena inteira na mesma pagina: virar a pagina no meio de uma fala e pior
     do que o espaco vazio embaixo. */
  .cena { break-inside: avoid; page-break-inside: avoid; padding: 16px 0; }
  .caixa, details, .tempo tr { break-inside: avoid; page-break-inside: avoid; }
  h2 { break-after: avoid; page-break-after: avoid; margin-top: 26px; }
  .capa { break-after: page; page-break-after: always; }

  .trilho { position: static; }

  h1 { font-size: 30pt; }
  .cena h3 { font-size: 14pt; }
  .fala { font-size: 11pt; }
  .tc { font-size: 15pt; }
  .acoes li { font-size: 10pt; }

  a { color: #00685d; text-decoration: none; }
  summary::before { content: "—"; }
</style>
"""

i = html.index('<div class="folha">')
corpo = html[i:html.rindex("</div>") + 6]
cabeca = html[:i]

(AQUI / "roteiro-impressao.html").write_text(f"""<!doctype html>
<html lang="pt-BR" data-theme="light">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
{cabeca}
{IMPRESSAO}
<style>
  /* As cores fazem parte da leitura: tarja de cena, faixa de fala e caixas de
     alerta perdem o sentido em cinza. */
  * {{ -webkit-print-color-adjust: exact; print-color-adjust: exact; }}
</style>
</head>
<body>
{corpo}
</body>
</html>""")

print("  roteiro-impressao.html montado")
