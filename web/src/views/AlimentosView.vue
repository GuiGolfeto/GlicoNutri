<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type { Alimento, CriarAlimento, ResultadoImportacao } from '../api/tipos'

const alimentos = ref<Alimento[]>([])
const grupos = ref<string[]>([])
const termo = ref('')
const grupo = ref('')
const carregando = ref(true)
const erro = ref('')
const sucesso = ref('')

const abrindoForm = ref(false)
const editando = ref<number | null>(null)
const enviando = ref(false)

const vazio = (): CriarAlimento => ({
  nome: '', grupoAlimentar: '',
  caloriasPor100g: null, carboidratosPor100g: null, proteinasPor100g: null,
  lipidiosPor100g: null, fibrasPor100g: null, indiceGlicemico: null,
})

const form = reactive<CriarAlimento>(vazio())

// Importação
const arquivo = ref<File | null>(null)
const importando = ref(false)
const relatorio = ref<ResultadoImportacao | null>(null)

let debounce: ReturnType<typeof setTimeout>
watch([termo, grupo], () => {
  clearTimeout(debounce)
  debounce = setTimeout(carregar, 250)
})

async function carregar() {
  carregando.value = true
  try {
    const { data } = await api.get<Alimento[]>('/alimentos', {
      params: { termo: termo.value || undefined, grupo: grupo.value || undefined, limite: 200 },
    })
    alimentos.value = data
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar os alimentos.')
  } finally {
    carregando.value = false
  }
}

async function carregarGrupos() {
  const { data } = await api.get<string[]>('/alimentos/grupos')
  grupos.value = data
}

onMounted(async () => { await Promise.all([carregar(), carregarGrupos()]) })

function abrirNovo() {
  Object.assign(form, vazio())
  editando.value = null
  abrindoForm.value = true
}

function abrirEdicao(a: Alimento) {
  Object.assign(form, {
    nome: a.nome, grupoAlimentar: a.grupoAlimentar,
    caloriasPor100g: a.caloriasPor100g, carboidratosPor100g: a.carboidratosPor100g,
    proteinasPor100g: a.proteinasPor100g, lipidiosPor100g: a.lipidiosPor100g,
    fibrasPor100g: a.fibrasPor100g, indiceGlicemico: a.indiceGlicemico,
  })
  editando.value = a.id
  abrindoForm.value = true
}

async function salvar() {
  erro.value = ''
  sucesso.value = ''
  enviando.value = true
  try {
    if (editando.value) await api.put(`/alimentos/${editando.value}`, form)
    else await api.post('/alimentos', form)

    abrindoForm.value = false
    sucesso.value = editando.value ? 'Alimento atualizado.' : 'Alimento cadastrado.'
    await Promise.all([carregar(), carregarGrupos()])
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}

/** RN12 — inativar tira das buscas, mas preserva a referência nos planos. */
async function inativar(a: Alimento) {
  erro.value = ''
  try {
    await api.delete(`/alimentos/${a.id}`)
    sucesso.value = `"${a.nome}" foi inativado e não aparece mais nas buscas.`
    await carregar()
  } catch (e) {
    erro.value = mensagemDeErro(e)
  }
}

function selecionarArquivo(evento: Event) {
  const alvo = evento.target as HTMLInputElement
  arquivo.value = alvo.files?.[0] ?? null
  relatorio.value = null
}

async function importar() {
  if (!arquivo.value) return
  erro.value = ''
  sucesso.value = ''
  relatorio.value = null
  importando.value = true

  const corpo = new FormData()
  corpo.append('arquivo', arquivo.value)

  try {
    const { data } = await api.post<ResultadoImportacao>('/alimentos/importar', corpo, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    relatorio.value = data
    arquivo.value = null
    await Promise.all([carregar(), carregarGrupos()])
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    importando.value = false
  }
}

const numero = (v: number | null) => (v === null ? '—' : v.toLocaleString('pt-BR'))
</script>

<template>
  <div class="entre cabecalho">
    <div>
      <h1>Banco de Alimentos</h1>
      <p class="sub">Base nutricional usada na montagem dos planos alimentares</p>
    </div>
    <button class="btn btn-primario" @click="abrirNovo">Novo Alimento</button>
  </div>

  <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>
  <div v-if="sucesso" class="aviso aviso-sucesso">{{ sucesso }}</div>

  <!-- RF01.2 / RN10 — importação em lote -->
  <section class="card importacao">
    <h3>Importar tabela TACO</h3>
    <p class="apoio">
      Arquivo CSV no padrão TACO/IBGE. Planilhas em XLS ou XLSX precisam ser
      exportadas como CSV antes do envio.
    </p>

    <div class="linha-importacao">
      <input type="file" accept=".csv,.txt" @change="selecionarArquivo" />
      <button class="btn btn-secundario" :disabled="!arquivo || importando" @click="importar">
        {{ importando ? 'Importando…' : 'Importar' }}
      </button>
    </div>

    <!-- Relatório de erros exigido pela RN10 -->
    <div v-if="relatorio" class="relatorio">
      <div class="resumo-importacao">
        <span><strong class="numerico">{{ relatorio.linhasLidas }}</strong> linhas lidas</span>
        <span class="ok"><strong class="numerico">{{ relatorio.importados }}</strong> importados</span>
        <span class="aviso-cor"><strong class="numerico">{{ relatorio.ignoradosPorDuplicidade }}</strong> duplicados</span>
        <span class="erro-cor"><strong class="numerico">{{ relatorio.rejeitados }}</strong> rejeitados</span>
      </div>

      <details v-if="relatorio.erros.length" open>
        <summary>{{ relatorio.erros.length }} linha(s) não importada(s)</summary>
        <ul class="erros">
          <li v-for="e in relatorio.erros.slice(0, 50)" :key="e.linha">
            <span class="numerico linha-num">linha {{ e.linha }}</span>
            <span class="motivo">{{ e.motivo }}</span>
            <code>{{ e.conteudo }}</code>
          </li>
        </ul>
        <p v-if="relatorio.erros.length > 50" class="apoio">
          Exibindo as 50 primeiras de {{ relatorio.erros.length }}.
        </p>
      </details>
    </div>
  </section>

  <!-- Cadastro manual (RF01.1) -->
  <section v-if="abrindoForm" class="card">
    <h3>{{ editando ? 'Editar alimento' : 'Novo alimento' }}</h3>
    <form @submit.prevent="salvar">
      <div class="grade-2">
        <div class="campo">
          <label for="nome">Nome *</label>
          <input id="nome" v-model="form.nome" required />
        </div>
        <div class="campo">
          <label for="grupo">Grupo alimentar</label>
          <input id="grupo" v-model="form.grupoAlimentar" list="grupos-existentes" />
          <datalist id="grupos-existentes">
            <option v-for="g in grupos" :key="g" :value="g" />
          </datalist>
        </div>
      </div>

      <p class="rotulo-secao">Valores por 100 g</p>
      <div class="grade-3">
        <div class="campo">
          <label for="kcal">Calorias (kcal)</label>
          <input id="kcal" v-model.number="form.caloriasPor100g" type="number" step="0.1" min="0" max="900" />
        </div>
        <div class="campo">
          <label for="cho">Carboidratos (g)</label>
          <input id="cho" v-model.number="form.carboidratosPor100g" type="number" step="0.1" min="0" max="100" />
        </div>
        <div class="campo">
          <label for="ptn">Proteínas (g)</label>
          <input id="ptn" v-model.number="form.proteinasPor100g" type="number" step="0.1" min="0" max="100" />
        </div>
        <div class="campo">
          <label for="lip">Lipídios (g)</label>
          <input id="lip" v-model.number="form.lipidiosPor100g" type="number" step="0.1" min="0" max="100" />
        </div>
        <div class="campo">
          <label for="fib">Fibras (g)</label>
          <input id="fib" v-model.number="form.fibrasPor100g" type="number" step="0.1" min="0" max="100" />
        </div>
        <div class="campo">
          <label for="ig">Índice glicêmico</label>
          <input id="ig" v-model.number="form.indiceGlicemico" type="number" min="0" max="200" />
          <span class="ajuda">
            A TACO não publica este dado. A referência do sistema são as International
            Tables of Glycemic Index and Glycemic Load Values 2021 — o que você informar
            aqui passa a constar como valor seu.
          </span>
        </div>
      </div>

      <div class="acoes">
        <button class="btn btn-primario" type="submit" :disabled="enviando || !form.nome">
          {{ enviando ? 'Salvando…' : 'Salvar' }}
        </button>
        <button class="btn btn-secundario" type="button" @click="abrindoForm = false">Cancelar</button>
      </div>
    </form>
  </section>

  <!-- Listagem -->
  <section class="card sem-padding">
    <div class="barra">
      <input v-model="termo" class="busca" type="search" placeholder="Buscar alimento… (ignora acentos)" />
      <select v-model="grupo" class="filtro-grupo">
        <option value="">Todos os grupos</option>
        <option v-for="g in grupos" :key="g" :value="g">{{ g }}</option>
      </select>
      <span class="contador numerico">{{ alimentos.length }}</span>
    </div>

    <p v-if="carregando" class="vazio">Carregando…</p>
    <p v-else-if="alimentos.length === 0" class="vazio">
      Nenhum alimento encontrado. Importe a tabela TACO ou cadastre manualmente.
    </p>

    <table v-else class="tabela">
      <thead>
        <tr>
          <th>Alimento</th><th>Grupo</th><th>kcal</th>
          <th>CHO</th><th>PTN</th><th>LIP</th><th>Fibra</th><th>IG</th><th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="a in alimentos" :key="a.id">
          <td>
            {{ a.nome }}
            <span class="fonte">{{ a.fonte }}</span>
          </td>
          <td>{{ a.grupoAlimentar || '—' }}</td>
          <td class="numerico">{{ numero(a.caloriasPor100g) }}</td>
          <td class="numerico">{{ numero(a.carboidratosPor100g) }}</td>
          <td class="numerico">{{ numero(a.proteinasPor100g) }}</td>
          <td class="numerico">{{ numero(a.lipidiosPor100g) }}</td>
          <td class="numerico">{{ numero(a.fibrasPor100g) }}</td>
          <td class="numerico">
            {{ numero(a.indiceGlicemico) }}
            <!-- Saber de onde veio o número importa tanto quanto o número. -->
            <span v-if="a.fonteIndiceGlicemico" class="origem-ig" :title="a.fonteIndiceGlicemico">
              {{ a.fonteIndiceGlicemico.startsWith('International') ? 'tabela' : 'informado' }}
            </span>
          </td>
          <td class="acao">
            <button class="btn btn-secundario" @click="abrirEdicao(a)">Editar</button>
            <button class="btn btn-perigo" @click="inativar(a)">Inativar</button>
          </td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<style scoped>
.origem-ig {
  display: block; font-size: 11px; color: var(--text-muted);
  letter-spacing: 0.03em; text-transform: uppercase;
}
.cabecalho { margin-bottom: var(--lg); }
.sub { margin: 4px 0 0; color: var(--text-secondary); font-size: 15px; }
.card { margin-bottom: var(--md); }
.card h3 { font-size: 15px; color: var(--text-secondary); margin-bottom: var(--xs); }
.apoio { font-size: 13px; color: var(--text-muted); margin: 0 0 var(--md); }

.linha-importacao { display: flex; align-items: center; gap: var(--sm); flex-wrap: wrap; }
.linha-importacao input[type=file] { font-size: 14px; }

.relatorio { margin-top: var(--md); padding-top: var(--md); border-top: 1px solid var(--border); }
.resumo-importacao { display: flex; gap: var(--lg); flex-wrap: wrap; font-size: 14px; margin-bottom: var(--sm); }
.resumo-importacao .ok { color: var(--success); }
.resumo-importacao .aviso-cor { color: var(--warning); }
.resumo-importacao .erro-cor { color: var(--danger); }

.erros { list-style: none; margin: var(--xs) 0 0; padding: 0; max-height: 260px; overflow-y: auto; }
.erros li { padding: 6px 0; border-bottom: 1px solid var(--border); font-size: 13px; }
.linha-num { color: var(--text-muted); margin-right: var(--xs); }
.motivo { color: var(--danger); }
.erros code {
  display: block; margin-top: 2px; font-size: 12px; color: var(--text-muted);
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}

.rotulo-secao { font-size: 13px; color: var(--text-muted); margin: var(--md) 0 var(--xs); }
.grade-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 0 var(--md); }
@media (max-width: 900px) { .grade-3 { grid-template-columns: 1fr 1fr; } }
.acoes { display: flex; gap: var(--sm); margin-top: var(--md); }

.sem-padding { padding: 0; }
.barra { display: flex; align-items: center; gap: var(--sm); padding: var(--md) var(--md) var(--sm); }
.busca, .filtro-grupo {
  font-family: inherit; font-size: 15px; padding: 9px 12px;
  border: 1px solid var(--border); border-radius: var(--raio); background: var(--background);
}
.busca { flex: 1; }
.busca:focus, .filtro-grupo:focus { outline: none; border-color: var(--primary); }
.contador { font-size: 13px; color: var(--text-muted); }

.fonte { display: block; font-size: 11px; letter-spacing: 0.05em; color: var(--text-muted); }
.acao { text-align: right; white-space: nowrap; }
.acao .btn { padding: 4px 10px; font-size: 13px; margin-left: 4px; }
</style>
