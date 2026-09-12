# -*- coding: utf-8 -*-
"""Monta o documento técnico do back-end a partir do código e do banco."""
import html, json, pathlib, datetime, re
import texto as T

RAIZ = pathlib.Path(__file__).parent
def e(x): return html.escape(str(x))

api = json.loads((RAIZ / "api.json").read_text())
servicos = json.loads((RAIZ / "servicos.json").read_text())

def psv(arquivo):
    linhas = (RAIZ / arquivo).read_text().strip().split("\n")
    return [l.split("|") for l in linhas if l.strip()]

colunas, constraints = psv("colunas.psv"), psv("constraints.psv")
indices, migrations = psv("indices.psv"), psv("migrations.psv")

# ── Índice das constraints por tabela ──
pks, fks, uniques, checks = {}, {}, {}, {}
for c in constraints:
    tabela, tipo, nome, cols = c[0], c[1], c[2], c[3]
    if tipo == "PRIMARY KEY": pks[tabela] = set(cols.split(","))
    elif tipo == "FOREIGN KEY": fks.setdefault(tabela, {})[cols] = f"{c[4]}.{c[5]}"
    elif tipo == "UNIQUE": uniques.setdefault(tabela, set()).update(cols.split(","))
    elif tipo == "CHECK" and c[6] and "NOT NULL" not in c[6]:
        checks.setdefault(tabela, []).append((nome, c[6]))

por_tabela = {}
for c in colunas:
    por_tabela.setdefault(c[0], []).append(c)

TIPOS = {"bigint": "bigint", "integer": "int", "boolean": "bool", "text": "text",
         "double precision": "float", "date": "date",
         "timestamp with time zone": "timestamptz",
         "time without time zone": "time", "character varying": "varchar"}

partes = []
hoje = datetime.date.today().strftime("%d de %B de %Y")
for en, pt in {"January":"janeiro","February":"fevereiro","March":"março","April":"abril",
               "May":"maio","June":"junho","July":"julho","August":"agosto",
               "September":"setembro","October":"outubro","November":"novembro",
               "December":"dezembro"}.items():
    hoje = hoje.replace(en, pt)

# ── Capa ──
meta = "".join(f"<div><dt>{e(k)}</dt><dd>{e(v)}</dd></div>" for k, v in T.CAPA["meta"])
partes.append(f"""
<div class="capa">
  <div class="marca">
    <svg class="gota" viewBox="0 0 40 40" fill="none" width="30" height="30">
      <path d="M20 38C27.732 38 34 31.732 34 24C34 16.268 20 2 20 2C20 2 6 16.268 6 24C6 31.732 12.268 38 20 38Z" fill="#00897B"/>
      <path d="M15 24L18.5 27.5L25.5 20.5" stroke="white" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
    </svg> GlicoNutri
  </div>
  <p class="sobretitulo">{e(T.CAPA['sobretitulo'])}</p>
  <h1>{e(T.CAPA['titulo'])}</h1>
  <p class="subtitulo">{e(T.CAPA['subtitulo'])}</p>
  <dl class="rodape">{meta}<div><dt>Emitido em</dt><dd>{e(hoje)}</dd></div></dl>
</div>""")

# ── Sumário ──
itens = "".join(f'<li><span class="titulo">{e(t)}</span><span class="desc">{e(d)}</span></li>'
                for t, d in T.SUMARIO)
partes.append(f'<div class="secao sumario"><h2>Sumário</h2><ol>{itens}</ol></div>')

n = 0
def sec(titulo, corpo):
    global n
    n += 1
    partes.append(f'<div class="secao"><h2><span class="numero">{n}</span>{e(titulo)}</h2>{corpo}</div>')

# ── 1. Arquitetura ──
A = T.ARQUITETURA
camadas = "".join(f"<tr><td><strong>{e(a)}</strong></td><td>{e(b)}</td></tr>" for a, b in A["camadas"])
fluxo = "".join(f"<tr><td><strong>{e(a)}</strong></td><td>{e(b)}</td></tr>" for a, b in A["fluxo"])
c4 = "".join(f"<tr><td class='mono'>{e(a)}</td><td>{e(b)}</td></tr>" for a, b in A["c4"])
sec("Arquitetura", f"""
  <p class="intro">{e(A['intro'])}</p>
  <h3>Camadas</h3>
  <table><thead><tr><th style="width:24%">Camada</th><th>Responsabilidade</th></tr></thead>
  <tbody>{camadas}</tbody></table>
  <h3>O caminho de uma requisição</h3>
  <table><thead><tr><th style="width:26%">Etapa</th><th>O que acontece</th></tr></thead>
  <tbody>{fluxo}</tbody></table>
  <h3>Correspondência com o diagrama de componentes</h3>
  <p>Os componentes desenhados no C4 e o que existe no código:</p>
  <table class="densa"><thead><tr><th style="width:40%">No diagrama</th><th>No código</th></tr></thead>
  <tbody>{c4}</tbody></table>""")

# ── 2. Tecnologias ──
tec = "".join(f"<tr><td><strong>{e(a)}</strong></td><td class='mono'>{e(b)}</td><td>{e(c)}</td></tr>"
              for a, b, c in T.TECNOLOGIAS["itens"])
sec("Tecnologias e dependências", f"""
  <p class="intro">{e(T.TECNOLOGIAS['intro'])}</p>
  <table class="densa"><thead><tr><th style="width:20%">Camada</th><th style="width:26%">Escolha</th>
  <th>Motivo</th></tr></thead><tbody>{tec}</tbody></table>""")

# ── 3. Modelo de dados ──
D = T.DADOS
grupos = ""
for titulo, tabelas, desc in D["grupos"]:
    nomes = ", ".join(tabelas)
    grupos += (f'<div class="grupo-tabelas"><h4>{e(titulo)}</h4>'
               f'<div class="nomes">{e(nomes)}</div><p>{e(desc)}</p></div>')
decisoes = "".join(f"<tr><td><strong>{e(a)}</strong></td><td>{e(b)}</td></tr>" for a, b in D["decisoes"])

# Tabela de resumo do schema
resumo = ""
for tabela in sorted(por_tabela):
    cols = por_tabela[tabela]
    pk = ", ".join(sorted(pks.get(tabela, [])))
    nfk = len(fks.get(tabela, {}))
    nchk = len(checks.get(tabela, []))
    resumo += (f"<tr><td class='mono'>{e(tabela)}</td><td class='numerico'>{len(cols)}</td>"
               f"<td class='mono'>{e(pk)}</td><td class='numerico'>{nfk}</td>"
               f"<td class='numerico'>{nchk or ''}</td></tr>")

# Detalhe de tabelas escolhidas
def detalhar(tabela):
    linhas = ""
    for _, col, tipo, tam, nulo, padrao in por_tabela.get(tabela, []):
        t = TIPOS.get(tipo, tipo) + (f"({tam})" if tam else "")
        marca = ""
        if col in pks.get(tabela, set()): marca = " class='coluna-pk'"
        elif col in [k for k in fks.get(tabela, {})]: marca = " class='coluna-fk'"
        notas = []
        if col in fks.get(tabela, {}): notas.append(f"→ {fks[tabela][col]}")
        if col in uniques.get(tabela, set()): notas.append("único")
        if nulo == "NO": notas.append("obrigatório")
        if padrao and "nextval" not in padrao: notas.append(f"padrão {padrao.split('::')[0]}")
        linhas += (f"<tr><td{marca}>{e(col)}</td><td class='mono'>{e(t)}</td>"
                   f"<td class='nulo'>{e(' · '.join(notas))}</td></tr>")
    return (f'<h4>{e(tabela)}</h4><table class="densa"><thead><tr><th style="width:32%">Coluna</th>'
            f'<th style="width:22%">Tipo</th><th>Notas</th></tr></thead><tbody>{linhas}</tbody></table>')

detalhes = "".join(detalhar(t) for t in
                   ["usuarios", "planos_alimentares", "registros_glicemia",
                    "resumo_clinico_paciente", "favoritos_conteudo"])

chks = "".join(f"<tr><td class='mono'>{e(t)}</td><td class='mono'>{e(c[0])}</td>"
               f"<td class='mono'>{e(c[1])}</td></tr>"
               for t in sorted(checks) for c in checks[t])

idx_parciais = "".join(f"<tr><td class='mono'>{e(i[0])}</td><td class='mono' style='font-size:7.5pt'>{e(i[2])}</td></tr>"
                       for i in indices if "WHERE" in i[2])

migs = "".join(f"<li class='mono'>{e(m[0])}</li>" for m in migrations)

sec("Modelo de dados", f"""
  <p class="intro">{e(D['intro'])}</p>
  <h3>Grupos de tabelas</h3>
  {grupos}
  <h3>Decisões de mapeamento</h3>
  <table><thead><tr><th style="width:30%">Decisão</th><th>Detalhe</th></tr></thead>
  <tbody>{decisoes}</tbody></table>
  <h3>As {len(por_tabela)} tabelas</h3>
  <table class="densa"><thead><tr><th style="width:34%">Tabela</th><th class="numerico">Colunas</th>
  <th style="width:24%">Chave primária</th><th class="numerico">FKs</th><th class="numerico">Checks</th>
  </tr></thead><tbody>{resumo}</tbody></table>
  <h3>Detalhe de tabelas centrais</h3>
  {detalhes}
  <h3>Restrições de verificação</h3>
  <p>Regras clínicas gravadas como constraint, válidas mesmo que a aplicação erre:</p>
  <table class="densa"><thead><tr><th style="width:26%">Tabela</th><th style="width:32%">Nome</th>
  <th>Condição</th></tr></thead><tbody>{chks}</tbody></table>
  <h3>Índices parciais</h3>
  <table class="densa"><thead><tr><th style="width:28%">Tabela</th><th>Definição</th></tr></thead>
  <tbody>{idx_parciais}</tbody></table>
  <h3>Migrations aplicadas</h3>
  <ul>{migs}</ul>""")

# ── 4. Segurança ──
seg = "".join(f"<tr><td><strong>{e(a)}</strong></td><td>{e(b)}</td></tr>" for a, b in T.SEGURANCA["itens"])
sec("Segurança e autenticação", f"""
  <p class="intro">{e(T.SEGURANCA['intro'])}</p>
  <table><thead><tr><th style="width:26%">Aspecto</th><th>Implementação</th></tr></thead>
  <tbody>{seg}</tbody></table>""")

# ── 5. Cálculos ──
formulas = ""
for nome, formula, nota in T.CALCULOS["formulas"]:
    formulas += (f'<h4>{e(nome)}</h4><div class="formula">{e(formula)}</div>'
                 f'<p>{e(nota)}</p>')
sec("Cálculos clínicos", f'<p class="intro">{e(T.CALCULOS["intro"])}</p>{formulas}')

# ── 6. Regras de negócio → serviços ──
regras_map = {}
for s in servicos:
    for r in s["regras"]:
        regras_map.setdefault(r, []).append(s["nome"])
linhas_regras = "".join(
    f"<li><span class='req rn'>{e(r)}</span> "
    f"<span class='mono'>{e(', '.join(sorted(set(regras_map[r]))))}</span></li>"
    for r in sorted(regras_map))
sec("Regras de negócio", f"""
  <p class="intro">Onde cada regra é aplicada. O código cita o número da regra no ponto exato em que
  ela é verificada, o que torna a rastreabilidade verificável por busca no repositório.</p>
  <ul class="duas-colunas">{linhas_regras}</ul>
  <p>As regras 31 e 34 não aparecem porque não são de código: a primeira depende da publicação com
  HTTPS, e a segunda é um compromisso operacional de disponibilidade.</p>""")

# ── 7. Serviços ──
linhas_serv = ""
for s in sorted(servicos, key=lambda x: -len(x["operacoes"])):
    if not s["operacoes"]: continue
    ops = ", ".join(o["nome"] for o in s["operacoes"][:9])
    if len(s["operacoes"]) > 9: ops += f" e mais {len(s['operacoes'])-9}"
    marcas = " ".join(f"<span class='req rn'>{e(r)}</span>" for r in s["regras"][:6])
    linhas_serv += (f"<tr><td class='mono'>{e(s['nome'])}</td>"
                    f"<td>{e(s['doc'][:230]) if s['doc'] else ''}<br>"
                    f"<span class='nulo mono'>{e(ops)}</span><br>{marcas}</td>"
                    f"<td class='numerico'>{s['linhas']}</td></tr>")
sec("Serviços", f"""
  <p class="intro">Os {len(servicos)} serviços da camada de negócio, ordenados por quantidade de
  operações. A coluna final traz o tamanho em linhas.</p>
  <table class="densa"><thead><tr><th style="width:24%">Serviço</th><th>Responsabilidade e operações</th>
  <th class="numerico">Linhas</th></tr></thead><tbody>{linhas_serv}</tbody></table>""")

# ── 8. API ──
blocos = ""
for c in sorted(api, key=lambda x: x["rota"]):
    acoes = ""
    for a in c["acoes"]:
        rota = f"{c['rota']}/{a['sufixo']}".rstrip("/")
        rota = re.sub(r"\{(\w+):\w+\}", r"{\1}", rota)
        corpo = f"<span class='nulo mono'>{e(a['corpo'])}</span>" if a["corpo"] else ""
        acoes += (f"<tr><td><span class='verbo {a['verbo'].lower()}'>{a['verbo']}</span></td>"
                  f"<td class='rota'>/{e(rota)}</td>"
                  f"<td>{e(a['doc'][:150]) if a['doc'] else ''} {corpo}</td>"
                  f"<td class='nulo'>{e(a['perfil'])}</td></tr>")
    blocos += (f"<h4>{e(c['nome'])}</h4>"
               f"{f'<p>{e(c[chr(100)+chr(111)+chr(99)])}</p>' if c['doc'] else ''}"
               f'<table class="densa"><thead><tr><th style="width:11%"></th>'
               f'<th style="width:34%">Rota</th><th>Descrição</th>'
               f'<th style="width:15%">Perfil</th></tr></thead><tbody>{acoes}</tbody></table>')
total = sum(len(c["acoes"]) for c in api)
sec("Superfície da API", f"""
  <p class="intro">Os {total} endpoints, agrupados por controlador. A coluna de perfil indica a
  política exigida; onde diz autenticado, a permissão é decidida por paciente.</p>
  {blocos}""")

# ── 9. TACO ──
desafios = "".join(f"<tr><td><strong>{e(a)}</strong></td><td>{e(b)}</td></tr>" for a, b in T.TACO["desafios"])
limites_taco = "".join(f"<tr><td><strong>{e(a)}</strong></td><td>{e(b)}</td></tr>" for a, b in T.TACO["limites"])
sec("Importação da tabela TACO", f"""
  <p class="intro">{e(T.TACO['intro'])}</p>
  <table><thead><tr><th style="width:24%">Particularidade</th><th>Como é tratada</th></tr></thead>
  <tbody>{desafios}</tbody></table>
  <h3>O que a fonte não fornece</h3>
  <table><thead><tr><th style="width:30%">Lacuna</th><th>Alcance</th></tr></thead>
  <tbody>{limites_taco}</tbody></table>""")

# ── 10. Modelo de leitura ──
como = "".join(f"<tr><td><strong>{e(a)}</strong></td><td>{e(b)}</td></tr>" for a, b in T.LEITURA["como"])
sec("Modelo de leitura do painel", f"""
  <p class="intro">{e(T.LEITURA['intro'])}</p>
  <table><thead><tr><th style="width:28%">Aspecto</th><th>Como funciona</th></tr></thead>
  <tbody>{como}</tbody></table>
  <div class="destaque"><div class="titulo">Consequência visível</div>
  <p>{e(T.LEITURA['efeito'])}</p></div>""")

# ── 11. Testes ──
sec("Testes", """
  <p class="intro">São 138 testes em duas camadas. Os unitários rodam em cerca de dois segundos e
  não dependem de banco; os de integração sobem a aplicação inteira contra um banco descartável,
  criado e destruído a cada execução.</p>
  <h3>Cobertura</h3>
  <table><thead><tr><th style="width:30%">Grupo</th><th>O que verifica</th></tr></thead><tbody>
  <tr><td><strong>Cálculos clínicos</strong></td><td>Índice de massa corporal e classificação,
  relação cintura-quadril, Harris-Benedict e Mifflin-St Jeor conferidas termo a termo, valor
  energético, distribuição de macronutrientes, classificação glicêmica e cálculo de idade,
  incluindo a convenção para nascidos em 29 de fevereiro.</td></tr>
  <tr><td><strong>Leitura da tabela nutricional</strong></td><td>Marcadores de traço e de valor
  não determinado, codificação Windows-1252, detecção de separador, reconhecimento de cabeçalhos
  em suas variações e a distinção entre título de seção e alimento sem valores.</td></tr>
  <tr><td><strong>Validações de entrada</strong></td><td>Dígito verificador do CPF com sequências
  repetidas e entradas malformadas, política de senha, hash com salt por chamada e geração de
  senha provisória sem caracteres ambíguos.</td></tr>
  <tr><td><strong>Regras de negócio</strong></td><td>Contra banco real: bloqueio progressivo nos
  três níveis, senha provisória, unicidade de e-mail contra conta desativada, pré-requisito do
  cálculo energético, soma dos macronutrientes, unicidade de plano ativo — incluindo a recusa do
  próprio banco a dois planos simultâneos — reclassificação do histórico glicêmico ao mudar a
  faixa alvo, e isolamento entre profissionais.</td></tr></tbody></table>
  <div class="destaque"><div class="titulo">Verificação por mutação</div>
  <p>Uma suíte que passa mas não falha quando o código quebra não protege nada. Foram introduzidos
  defeitos deliberados — divisão errada no índice de massa corporal, coeficiente alterado em
  Harris-Benedict, escalonamento do bloqueio achatado, reclassificação glicêmica removida, ordem
  de gravação do plano ativo invertida e marcador de traço tratado como ausência — e a suíte
  falhou em todos os casos.</p></div>""")

# ── 12. Configuração ──
sec("Configuração e execução", """
  <p class="intro">Nenhum segredo fica no repositório. A aplicação lê do armazenamento de segredos
  do .NET, e o repositório traz apenas o arquivo de exemplo com os nomes das chaves.</p>
  <h3>Chaves de configuração</h3>
  <table class="densa"><thead><tr><th style="width:34%">Chave</th><th>Função</th></tr></thead><tbody>
  <tr><td class="mono">ConnectionStrings:Supabase</td><td>Conexão com o banco. Usada por padrão.</td></tr>
  <tr><td class="mono">ConnectionStrings:Default</td><td>Banco local, usado quando USAR_LOCAL for verdadeiro.</td></tr>
  <tr><td class="mono">USAR_LOCAL</td><td>Alterna para o Postgres local, para trabalhar sem rede.</td></tr>
  <tr><td class="mono">Jwt:SigningKey</td><td>Chave de assinatura do token. Mínimo de 32 bytes.</td></tr>
  <tr><td class="mono">Jwt:Issuer · Jwt:Audience</td><td>Emissor e audiência validados no token.</td></tr>
  <tr><td class="mono">Jwt:ExpiresHours</td><td>Validade do token. Padrão de 24 horas.</td></tr>
  <tr><td class="mono">Email:Habilitado</td><td>Liga o envio por SMTP. Desligado, registra no log.</td></tr>
  <tr><td class="mono">Email:Host · Porta · Usuario · Senha</td><td>Credenciais do servidor de e-mail.</td></tr>
  <tr><td class="mono">Email:UrlWeb</td><td>Base usada para montar o link de redefinição de senha.</td></tr>
  <tr><td class="mono">Google:ClientId</td><td>Identificador do cliente OAuth. Sem ele o login federado fica desligado.</td></tr>
  </tbody></table>
  <h3>Execução</h3>
  <div class="formula">./dev.sh up          sobe API e front contra o Supabase
./dev.sh up --local  sobe o Postgres em Docker e aponta a API para ele
./dev.sh test        roda a suíte
./dev.sh logs api    acompanha o log</div>
  <h3>Ambiente em container</h3>
  <p>O banco de desenvolvimento sobe por Docker Compose, em um serviço PostgreSQL com volume
  nomeado e verificação de saúde. Quem for trabalhar no projeto não precisa instalar banco na
  máquina: o <span class="mono">./dev.sh up --local</span> levanta o container, aplica as
  migrations pendentes e sobe a API apontada para ele. A porta publicada é 5433, escolhida para
  não colidir com um PostgreSQL já instalado no sistema.</p>
  <p>A mesma containerização vale para a publicação: a API é distribuída como imagem, e é isso
  que determina quais hospedagens conseguem recebê-la.</p>
  <h3>Conexão com o Supabase</h3>
  <p>O host de conexão direta resolve apenas em IPv6 e nem sempre é alcançável. O acesso vai pelo
  pooler, que tem IPv4, e o usuário leva o identificador do projeto. O modo sessão, na porta 5432,
  é obrigatório para as migrations; o modo transação, na 6543, é mais eficiente em produção mas
  exige desabilitar prepared statements na string de conexão.</p>
  <h3>Primeiro acesso</h3>
  <p>O sistema não permite autocadastro: pacientes são criados por nutricionistas e nutricionistas
  por administradores. Em ambiente de desenvolvimento, um administrador inicial é criado
  automaticamente quando o banco está vazio, para a cadeia poder começar.</p>""")

# ── 13. Defeitos ──
defeitos = "".join(f"<tr><td><strong>{e(a)}</strong></td><td>{e(b)}</td></tr>" for a, b in T.DEFEITOS["itens"])
sec("Defeitos corrigidos", f"""
  <p class="intro">{e(T.DEFEITOS['intro'])}</p>
  <table><thead><tr><th style="width:26%">Defeito</th><th>O que acontecia</th></tr></thead>
  <tbody>{defeitos}</tbody></table>""")

# ── 14. Dívida técnica ──
divida = "".join(f"<tr><td><strong>{e(a)}</strong><br><span class='nao'>{e(b)}</span></td>"
                 f"<td>{e(c)}</td></tr>" for a, b, c in T.DIVIDA["itens"])
sec("Dívida técnica", f"""
  <p class="intro">{e(T.DIVIDA['intro'])}</p>
  <table><thead><tr><th style="width:30%">Item</th><th>Detalhe</th></tr></thead>
  <tbody>{divida}</tbody></table>""")

estilo = (RAIZ / "estilo.css").read_text()
def montar(corpo, extra=""):
    return (f'<!doctype html><html lang="pt-BR"><head><meta charset="utf-8">'
            f'<title>GlicoNutri — Back-end</title><style>{estilo}{extra}</style></head>'
            f'<body>{corpo}</body></html>')

SANGRIA = "@page { size: A4; margin: 0 !important; } body { margin: 0; }"
(RAIZ / "capa.html").write_text(montar(partes[0], SANGRIA), encoding="utf-8")
(RAIZ / "miolo.html").write_text(montar("".join(partes[1:])), encoding="utf-8")
print(f"  gerado: {n} seções, {total} endpoints, {len(por_tabela)} tabelas")
