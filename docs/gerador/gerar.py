# -*- coding: utf-8 -*-
"""Monta o HTML do relatório, com as telas embutidas em base64."""
import base64, html, datetime, pathlib, subprocess
import conteudo as C

RAIZ = pathlib.Path(__file__).parent
TELAS = RAIZ / "telas"

def imagem(nome):
    caminho = TELAS / f"{nome}.png"
    if not caminho.exists():
        return None, 0
    dados = base64.b64encode(caminho.read_bytes()).decode()
    saida = subprocess.run(["sips", "-g", "pixelWidth", "-g", "pixelHeight", str(caminho)],
                           capture_output=True, text=True).stdout
    nums = [int(l.split(":")[1]) for l in saida.split("\n") if ":" in l and l.strip().split(":")[0].strip() in ("pixelWidth","pixelHeight")]
    proporcao = (nums[1] / nums[0]) if len(nums) == 2 else 0.6
    return f"data:image/png;base64,{dados}", proporcao

def e(t): return html.escape(str(t))

def etiquetas(secao):
    partes = []
    for r in secao.get("requisitos", []): partes.append(f'<span class="req">{e(r)}</span>')
    for r in secao.get("regras", []): partes.append(f'<span class="req rn">{e(r)}</span>')
    for u in secao.get("casos", []): partes.append(f'<span class="req uc">{e(u)}</span>')
    return f'<p class="requisitos">{"".join(partes)}</p>' if partes else ""

def figura(nome, legenda):
    src, proporcao = imagem(nome)
    if not src: return ""
    # Tela alta ocupa a página inteira; as demais fluem no texto. Telas de
    # formulário centrado, capturadas estreitas, entram reduzidas para não
    # ocupar largura que não têm conteúdo para preencher.
    if proporcao > 1.3:
        classe, estilo_img = " class='alta'", ""
    elif proporcao > 0.85:
        classe, estilo_img = "", " style='max-width:112mm;margin:0 auto'"
    else:
        classe, estilo_img = "", ""
    return (f"<figure{classe}><img src='{src}'{estilo_img} alt='{e(legenda)}'>"
            f"<figcaption>{e(legenda)}</figcaption></figure>")

partes = []
hoje = datetime.date.today().strftime("%d de %B de %Y")
MESES = {"January":"janeiro","February":"fevereiro","March":"março","April":"abril","May":"maio",
         "June":"junho","July":"julho","August":"agosto","September":"setembro","October":"outubro",
         "November":"novembro","December":"dezembro"}
for en, pt in MESES.items(): hoje = hoje.replace(en, pt)

# ── Capa ──
meta = "".join(f"<div><dt>{e(k)}</dt><dd>{e(v)}</dd></div>" for k, v in C.CAPA["meta"])
partes.append(f"""
<div class="capa">
  <div class="marca"><span class="gota">◐</span> GlicoNutri</div>
  <p class="sobretitulo">{e(C.CAPA['sobretitulo'])}</p>
  <h1>{e(C.CAPA['titulo'])}</h1>
  <p class="subtitulo">{e(C.CAPA['subtitulo'])}</p>
  <dl class="rodape">{meta}
    <div><dt>Emitido em</dt><dd>{e(hoje)}</dd></div>
  </dl>
</div>""")

# ── Sumário ──
itens = "".join(f'<li><span class="titulo">{e(t)}</span><span class="desc">{e(d)}</span></li>'
                for t, d in C.SUMARIO)
partes.append(f'<div class="secao sumario"><h2>Sumário</h2><ol>{itens}</ol></div>')

# ── Situação da entrega ──
S = C.SITUACAO
entregue = "".join(f"<li>{e(x)}</li>" for x in S["entregue"])
decisoes_voce = "".join(
    f'<tr><td><strong>{e(t)}</strong></td><td>{e(d)}</td></tr>' for t, d in S["decisoes"])
divergencias = "".join(
    f'<tr><td><strong>{e(t)}</strong></td><td>{e(d)}</td></tr>' for t, d in S["divergencias"])
partes.append(f"""
<div class="secao">
  <h2><span class="numero">1</span>Situação da entrega</h2>
  <p class="intro">{e(S['intro'])}</p>

  <h3>Implementado</h3>
  <ul>{entregue}</ul>

  <h3>Decisões que dependem de você</h3>
  <p>Nenhuma delas impede o sistema de funcionar, mas todas precisam de resposta antes da
  entrega final. As duas primeiras são clínicas e mudam o resultado das prescrições.</p>
  <table><thead><tr><th style="width:30%">Assunto</th><th>Por quê</th></tr></thead>
  <tbody>{decisoes_voce}</tbody></table>

  <h3>Divergências encontradas na documentação</h3>
  <p>Ao implementar, apareceram pontos em que os documentos discordam entre si. Em cada caso o
  sistema seguiu a opção indicada abaixo, mas vale corrigir os documentos antes da defesa para
  que ninguém encontre a contradição primeiro.</p>
  <table><thead><tr><th style="width:30%">Ponto</th><th>Situação</th></tr></thead>
  <tbody>{divergencias}</tbody></table>
</div>""")

# ── Visão geral ──
v = C.VISAO_GERAL
ind = "".join(f'<div class="indicador"><div class="valor numerico">{e(n)}</div>'
              f'<div class="rotulo">{e(r)}</div></div>' for n, r in v["indicadores"])
camadas = "".join(f"<tr><td><strong>{e(n)}</strong></td><td>{e(t)}</td><td>{e(d)}</td></tr>"
                  for n, t, d in v["camadas"])
# Em tabela, e não em blocos soltos: três caixas com quebra proibida deixavam
# a última órfã numa página quase vazia.
decisoes = "".join(f"<tr><td><strong>{e(t)}</strong></td><td>{e(d)}</td></tr>"
                   for t, d in v["decisoes"])
partes.append(f"""
<div class="secao">
  <h2><span class="numero">2</span>Visão geral do sistema</h2>
  <p class="intro">{e(v['intro'])}</p>
  <div class="indicadores">{ind}</div>
  <h3>Camadas</h3>
  <table><thead><tr><th>Camada</th><th>Tecnologia</th><th>Responsabilidade</th></tr></thead>
  <tbody>{camadas}</tbody></table>
  <h3>Decisões de projeto que moldaram a implementação</h3>
  <table><thead><tr><th style="width:34%">Decisão</th><th>Consequência</th></tr></thead>
  <tbody>{decisoes}</tbody></table>
</div>""")

# ── Seções ──
for i, s in enumerate(C.SECOES, start=3):
    funcoes = "".join(f"<tr><td><strong>{e(n)}</strong></td><td>{e(d)}</td></tr>" for n, d in s["funcoes"])
    telas = "".join(figura(nome, legenda) for nome, legenda in s["telas"])
    partes.append(f"""
<div class="secao">
  <h2><span class="numero">{i}</span>{e(s['titulo'])}</h2>
  <p class="intro">{e(s['intro'])}</p>
  {etiquetas(s)}
  <h3>Funcionalidades</h3>
  <table><thead><tr><th style="width:32%">Recurso</th><th>Descrição</th></tr></thead>
  <tbody>{funcoes}</tbody></table>
  {telas}
</div>""")

# ── Validação ──
n = len(C.SECOES) + 3
grupos = "".join(f"<tr><td><strong>{e(t)}</strong></td><td>{e(d)}</td></tr>" for t, d in C.VALIDACAO["grupos"])
partes.append(f"""
<div class="secao">
  <h2><span class="numero">{n}</span>Validação automatizada</h2>
  <p class="intro">{e(C.VALIDACAO['intro'])}</p>
  <table><thead><tr><th style="width:30%">Grupo</th><th>Cobertura</th></tr></thead>
  <tbody>{grupos}</tbody></table>
  <div class="destaque"><div class="titulo">Verificação por mutação</div>
  <p>{e(C.VALIDACAO['mutacao'])}</p></div>
</div>""")

# ── Rastreabilidade ──
R = C.RASTREABILIDADE
reqs = "".join(
    f'<tr><td><span class="req">{e(c)}</span></td><td>{e(t)}</td><td>{e(o)}</td>'
    f'<td>{"<span class=\'ok\'>Completo</span>" if s_ == "Completo" else f"<span class=\'pendente\'>{e(s_)}</span>"}</td></tr>'
    for c, t, o, s_ in R["requisitos"])
regras = "".join(f'<tr><td><span class="req rn">{e(c)}</span></td><td>{e(t)}</td><td>{e(o)}</td></tr>'
                 for c, t, o in R["regras"])
partes.append(f"""
<div class="secao">
  <h2><span class="numero">{n+1}</span>Rastreabilidade</h2>
  <p class="intro">{e(R['intro'])}</p>
  <h3>Requisitos funcionais</h3>
  <table><thead><tr><th style="width:12%">Código</th><th style="width:34%">Requisito</th>
  <th style="width:32%">Onde</th><th>Situação</th></tr></thead><tbody>{reqs}</tbody></table>
  <h3>Regras de negócio</h3>
  <table><thead><tr><th style="width:12%">Código</th><th style="width:46%">Regra</th>
  <th>Onde</th></tr></thead><tbody>{regras}</tbody></table>
</div>""")

# ── Limites ──
limites = "".join(
    f"<tr><td><strong>{e(t)}</strong><br><span class='nao'>{e(m)}</span></td><td>{e(d)}</td></tr>"
    for t, m, d in C.LIMITES["itens"])
partes.append(f"""
<div class="secao">
  <h2><span class="numero">{n+2}</span>Limites conhecidos</h2>
  <p class="intro">{e(C.LIMITES['intro'])}</p>
  <table><thead><tr><th style="width:34%">Item</th><th>Situação</th></tr></thead>
  <tbody>{limites}</tbody></table>
</div>""")

estilo = (RAIZ / "estilo.css").read_text()
documento = f"""<!doctype html>
<html lang="pt-BR"><head><meta charset="utf-8">
<title>GlicoNutri — Funcionalidades Implementadas</title>
<style>{estilo}</style></head>
<body>{''.join(partes)}</body></html>"""

def montar(corpo, extra=""):
    return (f'<!doctype html><html lang="pt-BR"><head><meta charset="utf-8">'
            f'<title>GlicoNutri — Funcionalidades Implementadas</title>'
            f'<style>{estilo}{extra}</style></head><body>{corpo}</body></html>')

# A capa sangra: sem margem de pagina e sem folga do corpo.
SANGRIA = "@page { size: A4; margin: 0 !important; } body { margin: 0; }"
(RAIZ / "capa.html").write_text(montar(partes[0], SANGRIA), encoding="utf-8")
(RAIZ / "miolo.html").write_text(montar("".join(partes[1:])), encoding="utf-8")
print(f"  capa e miolo gerados ({len(documento)//1024} KB no total)")
