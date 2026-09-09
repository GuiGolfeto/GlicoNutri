<script setup lang="ts">
import { onMounted, reactive, ref, watch } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type { Alimento, ConteudoEducativo, Receita, Referencia } from '../api/tipos'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()

const secao = ref<'receitas' | 'conteudos'>('receitas')
const receitas = ref<Receita[]>([])
const conteudos = ref<ConteudoEducativo[]>([])
const tipos = ref<Referencia[]>([])

const termo = ref('')
const ingrediente = ref('')
const carregando = ref(true)
const erro = ref('')
const sucesso = ref('')
const enviando = ref(false)

const formReceita = reactive({
  nome: '', descricao: '', tempoPreparo: null as number | null,
  porcoes: null as number | null, instrucoes: '',
  ingredientes: [] as { alimentoId: number; quantidade: number; unidade: string; nome: string }[],
})

const formConteudo = reactive({ titulo: '', tipoId: null as number | null, corpo: '' })
const abrindo = ref<'receita' | 'conteudo' | null>(null)

// Quando preenchido, o formulário está editando em vez de criar.
const editandoReceita = ref<number | null>(null)
const editandoConteudo = ref<number | null>(null)

// Seletor de ingredientes
const busca = ref('')
const resultados = ref<Alimento[]>([])
const selecionado = ref<Alimento | null>(null)
const quantidade = ref<number | null>(100)

let debounceBusca: ReturnType<typeof setTimeout>
watch(busca, () => {
  clearTimeout(debounceBusca)
  debounceBusca = setTimeout(async () => {
    if (busca.value.trim().length < 2) { resultados.value = []; return }
    const { data } = await api.get<Alimento[]>('/alimentos', { params: { termo: busca.value, limite: 10 } })
    resultados.value = data
  }, 250)
})

let debounceFiltro: ReturnType<typeof setTimeout>
watch([termo, ingrediente], () => {
  clearTimeout(debounceFiltro)
  debounceFiltro = setTimeout(carregar, 250)
})

async function carregar() {
  carregando.value = true
  try {
    const [r, c] = await Promise.all([
      api.get<Receita[]>('/receitas', {
        params: {
          termo: termo.value || undefined,
          ingrediente: ingrediente.value || undefined,
          // RN29 — só quem despublica enxerga o que está fora do ar.
          incluirInativas: auth.ehAdministrador || undefined,
        },
      }),
      api.get<ConteudoEducativo[]>('/conteudos', {
        params: { termo: termo.value || undefined, incluirInativos: auth.ehAdministrador || undefined },
      }),
    ])
    receitas.value = r.data
    conteudos.value = c.data
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar o repositório.')
  } finally {
    carregando.value = false
  }
}

onMounted(async () => {
  const { data } = await api.get<Referencia[]>('/referencias/tipos-conteudo')
  tipos.value = data
  formConteudo.tipoId = data[0]?.id ?? null
  await carregar()
})

function adicionarIngrediente() {
  if (!selecionado.value || !quantidade.value) return
  formReceita.ingredientes.push({
    alimentoId: selecionado.value.id,
    quantidade: quantidade.value,
    unidade: 'g',
    nome: selecionado.value.nome,
  })
  selecionado.value = null
  busca.value = ''
  resultados.value = []
  quantidade.value = 100
}

function editarReceita(r: Receita) {
  Object.assign(formReceita, {
    nome: r.nome,
    descricao: r.descricao ?? '',
    tempoPreparo: r.tempoPreparo,
    porcoes: r.porcoes,
    instrucoes: r.instrucoes ?? '',
    ingredientes: r.ingredientes.map((i) => ({
      alimentoId: i.alimentoId, quantidade: i.quantidade,
      unidade: i.unidade ?? 'g', nome: i.alimentoNome,
    })),
  })
  editandoReceita.value = r.id
  abrindo.value = 'receita'
}

function editarConteudo(c: ConteudoEducativo) {
  Object.assign(formConteudo, { titulo: c.titulo, tipoId: c.tipoId, corpo: c.corpo ?? '' })
  editandoConteudo.value = c.id
  abrindo.value = 'conteudo'
}

function fecharFormulario() {
  abrindo.value = null
  editandoReceita.value = null
  editandoConteudo.value = null
  Object.assign(formReceita, {
    nome: '', descricao: '', tempoPreparo: null, porcoes: null, instrucoes: '', ingredientes: [],
  })
  formConteudo.titulo = ''
  formConteudo.corpo = ''
}

async function salvarReceita() {
  erro.value = ''
  enviando.value = true
  try {
    const corpo = {
      ...formReceita,
      ingredientes: formReceita.ingredientes.map(({ nome, ...i }) => i),
    }
    if (editandoReceita.value) await api.put(`/receitas/${editandoReceita.value}`, corpo)
    else await api.post('/receitas', corpo)

    const acao = editandoReceita.value ? 'atualizada' : 'publicada'
    fecharFormulario()
    sucesso.value = `Receita ${acao}.`
    await carregar()
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}

async function salvarConteudo() {
  erro.value = ''
  enviando.value = true
  try {
    if (editandoConteudo.value) await api.put(`/conteudos/${editandoConteudo.value}`, formConteudo)
    else await api.post('/conteudos', formConteudo)

    const acao = editandoConteudo.value ? 'atualizado' : 'publicado'
    fecharFormulario()
    sucesso.value = `Conteúdo ${acao}.`
    await carregar()
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}

/** RN29 — despublicar é exclusivo do Administrador. */
async function despublicar(tipo: 'receitas' | 'conteudos', id: number) {
  erro.value = ''
  try {
    await api.delete(`/${tipo}/${id}`)
    sucesso.value = 'Item despublicado. Ele continua na base e pode ser republicado.'
    await carregar()
  } catch (e) {
    erro.value = mensagemDeErro(e)
  }
}

/** RN29 — a despublicação é lógica, então republicar devolve o item ao ar. */
async function republicar(tipo: 'receitas' | 'conteudos', id: number) {
  erro.value = ''
  try {
    await api.post(`/${tipo}/${id}/republicar`, {})
    sucesso.value = 'Item republicado.'
    await carregar()
  } catch (e) {
    erro.value = mensagemDeErro(e)
  }
}
</script>

<template>
  <div class="entre cabecalho">
    <div>
      <h1>Repositório Educativo</h1>
      <p class="sub">Receitas e conteúdos disponíveis aos pacientes no aplicativo</p>
    </div>
    <div class="linha">
      <button class="btn btn-secundario" @click="fecharFormulario(); abrindo = 'conteudo'">
        Novo conteúdo
      </button>
      <button class="btn btn-primario" @click="fecharFormulario(); abrindo = 'receita'">
        Nova receita
      </button>
    </div>
  </div>

  <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>
  <div v-if="sucesso" class="aviso aviso-sucesso">{{ sucesso }}</div>

  <!-- RF09.1 — cadastro de receita -->
  <section v-if="abrindo === 'receita'" class="card">
    <h3>{{ editandoReceita ? 'Editar receita' : 'Nova receita' }}</h3>
    <form @submit.prevent="salvarReceita">
      <div class="campo">
        <label for="rnome">Nome *</label>
        <input id="rnome" v-model="formReceita.nome" required />
      </div>

      <div class="grade-3">
        <div class="campo">
          <label for="tempo">Tempo de preparo (min)</label>
          <input id="tempo" v-model.number="formReceita.tempoPreparo" type="number" min="1" max="1440" />
        </div>
        <div class="campo">
          <label for="porcoes">Porções</label>
          <input id="porcoes" v-model.number="formReceita.porcoes" type="number" min="1" max="100" />
        </div>
      </div>

      <div class="campo">
        <label for="descricao">Descrição</label>
        <input id="descricao" v-model="formReceita.descricao" maxlength="1000" />
      </div>

      <div class="campo">
        <label for="instrucoes">Modo de preparo *</label>
        <textarea id="instrucoes" v-model="formReceita.instrucoes" rows="4" required minlength="10"></textarea>
      </div>

      <!-- RN30 — ingredientes vinculados a alimentos da base -->
      <p class="rotulo-secao">Ingredientes *</p>
      <div class="seletor">
        <div class="autocomplete">
          <input v-model="busca" placeholder="Buscar alimento na base…" />
          <ul v-if="resultados.length && !selecionado" class="sugestoes">
            <li v-for="a in resultados" :key="a.id" @click="selecionado = a; busca = a.nome; resultados = []">
              {{ a.nome }}
            </li>
          </ul>
        </div>
        <input v-model.number="quantidade" type="number" min="1" class="qtd" placeholder="g" />
        <button class="btn btn-secundario pequeno" type="button"
                :disabled="!selecionado || !quantidade" @click="adicionarIngrediente">
          Adicionar
        </button>
      </div>

      <ul v-if="formReceita.ingredientes.length" class="ingredientes">
        <li v-for="(i, idx) in formReceita.ingredientes" :key="idx">
          <span class="nome">{{ i.nome }}</span>
          <span class="numerico">{{ i.quantidade }} g</span>
          <button class="remover" type="button" @click="formReceita.ingredientes.splice(idx, 1)">remover</button>
        </li>
      </ul>
      <p v-else class="ajuda">
        A receita precisa de ao menos um ingrediente para os valores nutricionais serem calculados.
      </p>

      <div class="acoes">
        <button class="btn btn-primario" type="submit"
                :disabled="enviando || !formReceita.nome || formReceita.ingredientes.length === 0">
          {{ enviando ? 'Salvando…' : editandoReceita ? 'Salvar alterações' : 'Publicar receita' }}
        </button>
        <button class="btn btn-secundario" type="button" @click="fecharFormulario">Cancelar</button>
      </div>
    </form>
  </section>

  <!-- RF09.2 — conteúdo educativo -->
  <section v-if="abrindo === 'conteudo'" class="card">
    <h3>{{ editandoConteudo ? 'Editar conteúdo' : 'Novo conteúdo educativo' }}</h3>
    <form @submit.prevent="salvarConteudo">
      <div class="grade-2">
        <div class="campo">
          <label for="ctitulo">Título *</label>
          <input id="ctitulo" v-model="formConteudo.titulo" required minlength="3" />
        </div>
        <div class="campo">
          <label for="ctipo">Tipo *</label>
          <select id="ctipo" v-model="formConteudo.tipoId" required>
            <option v-for="t in tipos" :key="t.id" :value="t.id">{{ t.descricao }}</option>
          </select>
        </div>
      </div>
      <div class="campo">
        <label for="ccorpo">Texto *</label>
        <textarea id="ccorpo" v-model="formConteudo.corpo" rows="8" required minlength="10"></textarea>
        <span class="ajuda">Aceita Markdown para formatação.</span>
      </div>
      <div class="acoes">
        <button class="btn btn-primario" type="submit" :disabled="enviando || !formConteudo.titulo">
          {{ enviando ? 'Salvando…' : editandoConteudo ? 'Salvar alterações' : 'Publicar conteúdo' }}
        </button>
        <button class="btn btn-secundario" type="button" @click="fecharFormulario">Cancelar</button>
      </div>
    </form>
  </section>

  <!-- Listagem -->
  <nav class="abas">
    <button class="aba" :class="{ ativa: secao === 'receitas' }" @click="secao = 'receitas'">
      Receitas ({{ receitas.length }})
    </button>
    <button class="aba" :class="{ ativa: secao === 'conteudos' }" @click="secao = 'conteudos'">
      Conteúdos ({{ conteudos.length }})
    </button>
  </nav>

  <div class="filtros">
    <input v-model="termo" class="busca" type="search" placeholder="Buscar por título…" />
    <input v-if="secao === 'receitas'" v-model="ingrediente" class="busca"
           type="search" placeholder="Buscar por ingrediente…" />
  </div>

  <p v-if="carregando" class="vazio">Carregando…</p>

  <template v-else-if="secao === 'receitas'">
    <p v-if="receitas.length === 0" class="vazio">Nenhuma receita publicada.</p>

    <article v-for="r in receitas" :key="r.id" class="card receita" :class="{ inativo: !r.ativo }">
      <div class="entre">
        <div>
          <h3>{{ r.nome }}</h3>
          <p class="meta numerico">
            <template v-if="r.porcoes">{{ r.porcoes }} porções · </template>
            <template v-if="r.tempoPreparo">{{ r.tempoPreparo }} min · </template>
            por {{ r.nutricionistaNome }}
          </p>
        </div>
        <div class="linha acoes-item">
          <span v-if="!r.ativo" class="selo selo-neutro">Despublicada</span>
          <button class="btn btn-secundario" @click="editarReceita(r)">Editar</button>
          <template v-if="auth.ehAdministrador">
            <button v-if="r.ativo" class="btn btn-perigo" @click="despublicar('receitas', r.id)">
              Despublicar
            </button>
            <button v-else class="btn btn-secundario" @click="republicar('receitas', r.id)">
              Republicar
            </button>
          </template>
        </div>
      </div>

      <p v-if="r.descricao" class="descricao">{{ r.descricao }}</p>

      <div class="nutricional">
        <span><strong class="numerico">{{ r.nutricional.calorias }}</strong> kcal totais</span>
        <span v-if="r.nutricional.caloriasPorPorcao">
          <strong class="numerico">{{ r.nutricional.caloriasPorPorcao }}</strong> kcal/porção
        </span>
        <span>CHO <strong class="numerico">{{ r.nutricional.carboidratos }}</strong> g</span>
        <span>PTN <strong class="numerico">{{ r.nutricional.proteinas }}</strong> g</span>
        <span>Fibra <strong class="numerico">{{ r.nutricional.fibras }}</strong> g</span>
      </div>

      <!-- A base tem lacunas; o total sai subestimado e isso precisa aparecer -->
      <p v-if="!r.nutricional.completa" class="incompleta">
        Algum ingrediente não tem todos os valores na base nutricional — os totais
        acima estão subestimados.
      </p>

      <details>
        <summary>{{ r.ingredientes.length }} ingrediente(s) e modo de preparo</summary>
        <ul class="ingredientes">
          <li v-for="i in r.ingredientes" :key="i.id">
            <span class="nome">{{ i.alimentoNome }}</span>
            <span class="numerico">{{ i.quantidade }} {{ i.unidade }}</span>
            <span class="numerico kcal">{{ i.calorias ?? '—' }} kcal</span>
          </li>
        </ul>
        <p class="preparo">{{ r.instrucoes }}</p>
      </details>
    </article>
  </template>

  <template v-else>
    <p v-if="conteudos.length === 0" class="vazio">Nenhum conteúdo publicado.</p>

    <article v-for="c in conteudos" :key="c.id" class="card" :class="{ inativo: !c.ativo }">
      <div class="entre">
        <div>
          <h3>{{ c.titulo }}</h3>
          <p class="meta">
            <span class="selo selo-neutro">{{ c.tipo }}</span>
            por {{ c.autorNome }}
          </p>
        </div>
        <div class="linha acoes-item">
          <span v-if="!c.ativo" class="selo selo-neutro">Despublicado</span>
          <button class="btn btn-secundario" @click="editarConteudo(c)">Editar</button>
          <template v-if="auth.ehAdministrador">
            <button v-if="c.ativo" class="btn btn-perigo" @click="despublicar('conteudos', c.id)">
              Despublicar
            </button>
            <button v-else class="btn btn-secundario" @click="republicar('conteudos', c.id)">
              Republicar
            </button>
          </template>
        </div>
      </div>
      <p class="corpo">{{ c.corpo }}</p>
    </article>
  </template>
</template>

<style scoped>
.cabecalho { margin-bottom: var(--lg); }
.sub { margin: 4px 0 0; color: var(--text-secondary); font-size: 15px; }
.card { margin-bottom: var(--md); }
.acoes-item .btn { padding: 5px 12px; font-size: 13px; }
.inativo { opacity: 0.6; }
.card h3 { font-size: 16px; margin-bottom: 2px; }
.meta { margin: 0 0 var(--sm); font-size: 13px; color: var(--text-muted); }

.rotulo-secao { font-size: 13px; color: var(--text-muted); margin: var(--md) 0 var(--xs); }
.grade-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: 0 var(--md); }
.acoes { display: flex; gap: var(--sm); margin-top: var(--md); }
.btn.pequeno { padding: 6px 14px; font-size: 14px; }

.seletor { display: flex; gap: var(--xs); align-items: flex-start; }
.seletor input {
  font-family: inherit; font-size: 14px; padding: 8px 10px;
  border: 1px solid var(--border); border-radius: var(--raio); background: var(--background);
}
.autocomplete { position: relative; flex: 1; }
.autocomplete input { width: 100%; }
.qtd { width: 80px; }
.sugestoes {
  position: absolute; z-index: 10; top: 100%; left: 0; right: 0;
  list-style: none; margin: 2px 0 0; padding: 4px;
  background: var(--surface); border: 1px solid var(--border);
  border-radius: var(--raio); max-height: 220px; overflow-y: auto;
}
.sugestoes li { padding: 7px 10px; border-radius: 6px; cursor: pointer; font-size: 14px; }
.sugestoes li:hover { background: var(--hover); }

.ingredientes { list-style: none; margin: var(--sm) 0 0; padding: 0; }
.ingredientes li {
  display: flex; align-items: center; gap: var(--sm);
  padding: 5px 0; border-bottom: 1px solid var(--border); font-size: 14px;
}
.nome { flex: 1; }
.kcal { color: var(--text-muted); font-size: 13px; }
.remover { background: none; border: none; color: var(--danger); font-family: inherit; font-size: 12px; cursor: pointer; }

.abas { display: flex; gap: 2px; border-bottom: 1px solid var(--border); margin-bottom: var(--md); }
.aba {
  font-family: inherit; font-size: 15px; padding: 9px 16px;
  background: none; border: none; border-bottom: 2px solid transparent;
  color: var(--text-secondary); cursor: pointer; margin-bottom: -1px;
}
.aba.ativa { color: var(--primary); border-bottom-color: var(--primary); font-weight: 500; }

.filtros { display: flex; gap: var(--sm); margin-bottom: var(--md); }
.busca {
  flex: 1; font-family: inherit; font-size: 15px; padding: 9px 12px;
  border: 1px solid var(--border); border-radius: var(--raio); background: var(--surface);
}

.descricao { font-size: 14px; color: var(--text-secondary); margin: 0 0 var(--sm); }
.nutricional { display: flex; gap: var(--lg); flex-wrap: wrap; font-size: 14px; margin-bottom: var(--xs); }
.incompleta { font-size: 13px; color: var(--warning); margin: 0 0 var(--sm); }
.preparo { font-size: 14px; color: var(--text-secondary); white-space: pre-wrap; margin-top: var(--sm); }
.corpo { font-size: 15px; color: var(--text-secondary); white-space: pre-wrap; margin: 0; }
details summary { font-size: 13px; color: var(--text-muted); cursor: pointer; }
</style>
