<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type {
  Alimento, Distribuicao, FaixasMacronutrientes, ItemPlanoNovo, PlanoAlimentar,
} from '../api/tipos'

const props = defineProps<{ pacienteId: number; recarregar: number }>()

const plano = ref<PlanoAlimentar | null>(null)
const historico = ref<PlanoAlimentar[]>([])
const carregando = ref(true)
const erro = ref('')
const bloqueio = ref('')

const montando = ref(false)
const salvando = ref(false)

const REFEICOES = ['Café da manhã', 'Lanche da manhã', 'Almoço', 'Lanche da tarde', 'Jantar', 'Ceia']

const form = reactive({
  objetivo: '',
  dataInicio: new Date().toISOString().slice(0, 10),
  observacoes: '',
  // Sobrescritos pelas faixas da SBD assim que a referência chega do servidor.
  carboidratosPercentual: 50,
  proteinasPercentual: 20,
  lipidiosPercentual: 30,
})

const itens = ref<(ItemPlanoNovo & { nome: string })[]>([])
const previa = ref<Distribuicao | null>(null)

// Seletor de alimentos
const busca = ref('')
const resultados = ref<Alimento[]>([])
const selecionado = ref<Alimento | null>(null)
const quantidade = ref<number | null>(100)
const refeicaoAtual = ref(REFEICOES[0])

/**
 * UC007 A2 — faixas da diretriz da SBD, vindas do servidor para não existirem
 * dois lugares dizendo qual é o padrão clínico.
 */
const faixas = ref<FaixasMacronutrientes | null>(null)

/** Fora da faixa a tela avisa, mas não impede: a conduta é do nutricionista. */
function foraDaFaixa(valor: number, qual: 'carboidratos' | 'proteinas' | 'lipidios') {
  const faixa = faixas.value?.[qual]
  if (!faixa) return false
  return valor < faixa.minimo || valor > faixa.maximo
}

const algumForaDaFaixa = computed(() =>
  foraDaFaixa(form.carboidratosPercentual, 'carboidratos')
  || foraDaFaixa(form.proteinasPercentual, 'proteinas')
  || foraDaFaixa(form.lipidiosPercentual, 'lipidios'))

const soma = computed(() =>
  Math.round((form.carboidratosPercentual + form.proteinasPercentual + form.lipidiosPercentual) * 100) / 100)

const somaValida = computed(() => Math.abs(soma.value - 100) < 0.01)

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const [ativo, hist] = await Promise.all([
      api.get<PlanoAlimentar | ''>(`/pacientes/${props.pacienteId}/plano-alimentar`),
      api.get<PlanoAlimentar[]>(`/pacientes/${props.pacienteId}/plano-alimentar/historico`),
    ])
    plano.value = ativo.data || null
    historico.value = hist.data
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar o plano alimentar.')
  } finally {
    carregando.value = false
  }
}

onMounted(async () => {
  await carregar()

  // Sem as faixas o formulário ainda funciona; só deixa de orientar.
  try {
    const { data } = await api.get<FaixasMacronutrientes>('/referencias/faixas-macronutrientes')
    faixas.value = data

    // Plano ainda não montado: os campos partem do padrão da diretriz.
    if (!plano.value) {
      form.carboidratosPercentual = data.carboidratos.padrao
      form.proteinasPercentual = data.proteinas.padrao
      form.lipidiosPercentual = data.lipidios.padrao
    }
  } catch {
    // referência indisponível: segue com o padrão compilado no formulário
  }
})
watch(() => props.recarregar, carregar)

/** UC007 A2 — prévia da distribuição sobre o VET vigente. */
async function calcularPrevia() {
  bloqueio.value = ''
  previa.value = null
  if (!somaValida.value) return

  try {
    const { data } = await api.post<Distribuicao>(
      `/pacientes/${props.pacienteId}/plano-alimentar/distribuicao`,
      {
        carboidratosPercentual: form.carboidratosPercentual,
        proteinasPercentual: form.proteinasPercentual,
        lipidiosPercentual: form.lipidiosPercentual,
      })
    previa.value = data
  } catch (e) {
    const msg = mensagemDeErro(e)
    // RN13 — sem VET não há plano.
    if (msg.includes('energética')) bloqueio.value = msg
    else erro.value = msg
  }
}

async function abrirMontagem() {
  montando.value = true
  itens.value = []
  form.objetivo = ''
  await calcularPrevia()
}

/** Copia o plano vigente como ponto de partida (UC007 A3). */
function duplicarAtual() {
  if (!plano.value) return
  montando.value = true
  form.objetivo = plano.value.objetivo ?? ''
  if (plano.value.distribuicao) {
    form.carboidratosPercentual = plano.value.distribuicao.carboidratosPercentual
    form.proteinasPercentual = plano.value.distribuicao.proteinasPercentual
    form.lipidiosPercentual = plano.value.distribuicao.lipidiosPercentual
  }
  itens.value = plano.value.itens.map((i) => ({
    alimentoId: i.alimentoId, dia: i.dia, horario: i.horario,
    refeicao: i.refeicao, quantidade: i.quantidade, unidade: i.unidade, nome: i.alimentoNome,
  }))
  calcularPrevia()
}

let debounce: ReturnType<typeof setTimeout>
watch(busca, () => {
  clearTimeout(debounce)
  debounce = setTimeout(async () => {
    if (busca.value.trim().length < 2) { resultados.value = []; return }
    const { data } = await api.get<Alimento[]>('/alimentos', {
      params: { termo: busca.value, limite: 12 },
    })
    resultados.value = data
  }, 250)
})

function adicionarItem() {
  if (!selecionado.value || !quantidade.value) return

  itens.value.push({
    alimentoId: selecionado.value.id,
    dia: 1,
    refeicao: refeicaoAtual.value,
    quantidade: quantidade.value,
    unidade: 'g',
    nome: selecionado.value.nome,
  })

  selecionado.value = null
  busca.value = ''
  resultados.value = []
  quantidade.value = 100
}

const caloriasDoItem = (item: ItemPlanoNovo & { nome: string }) => {
  const alimento = resultados.value.find((a) => a.id === item.alimentoId)
  return alimento?.caloriasPor100g != null
    ? Math.round((alimento.caloriasPor100g * item.quantidade) / 100)
    : null
}

const porRefeicao = computed(() => {
  const grupos = new Map<string, (ItemPlanoNovo & { nome: string })[]>()
  for (const item of itens.value) {
    const chave = item.refeicao ?? 'Sem refeição'
    if (!grupos.has(chave)) grupos.set(chave, [])
    grupos.get(chave)!.push(item)
  }
  return [...grupos.entries()]
})

async function salvarPlano() {
  erro.value = ''
  salvando.value = true
  try {
    await api.post(`/pacientes/${props.pacienteId}/plano-alimentar`, {
      objetivo: form.objetivo,
      dataInicio: form.dataInicio,
      observacoes: form.observacoes || null,
      distribuicao: {
        carboidratosPercentual: form.carboidratosPercentual,
        proteinasPercentual: form.proteinasPercentual,
        lipidiosPercentual: form.lipidiosPercentual,
      },
      itens: itens.value.map(({ nome, ...resto }) => resto),
    })
    montando.value = false
    await carregar()
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    salvando.value = false
  }
}

async function reativar(id: number) {
  erro.value = ''
  try {
    await api.post(`/pacientes/${props.pacienteId}/plano-alimentar/${id}/reativar`, {})
    await carregar()
  } catch (e) {
    erro.value = mensagemDeErro(e)
  }
}

const data = (iso: string) => new Date(iso + 'T12:00:00').toLocaleDateString('pt-BR')
</script>

<template>
  <section class="card">
    <div class="entre topo">
      <h3>Plano alimentar</h3>
      <div class="linha" v-if="!montando">
        <button v-if="plano" class="btn btn-secundario pequeno" @click="duplicarAtual">Duplicar</button>
        <button class="btn btn-secundario pequeno" @click="abrirMontagem">Novo plano</button>
      </div>
    </div>

    <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>
    <div v-if="bloqueio" class="aviso aviso-atencao">{{ bloqueio }}</div>

    <p v-if="carregando" class="vazio">Carregando…</p>

    <!-- ── Montagem ── -->
    <template v-else-if="montando">
      <div class="grade-2">
        <div class="campo">
          <label for="objetivo">Objetivo do plano *</label>
          <input id="objetivo" v-model="form.objetivo" required
                 placeholder="Ex.: controle glicêmico e manutenção de peso" />
        </div>
        <div class="campo">
          <label for="inicio">Data de início *</label>
          <input id="inicio" v-model="form.dataInicio" type="date" required />
        </div>
      </div>

      <div class="entre cabecalho-macros">
        <p class="rotulo-secao">Distribuição de macronutrientes</p>
        <span v-if="faixas" class="fonte-faixa">{{ faixas.referencia }}</span>
      </div>
      <div class="macros">
        <div class="campo">
          <label for="cho">Carboidratos (%)</label>
          <input id="cho" v-model.number="form.carboidratosPercentual" type="number" step="0.5"
                 min="0" max="100" :class="{ atencao: foraDaFaixa(form.carboidratosPercentual, 'carboidratos') }"
                 @change="calcularPrevia" />
          <span v-if="faixas" class="ajuda">{{ faixas.carboidratos.minimo }}–{{ faixas.carboidratos.maximo }}%</span>
        </div>
        <div class="campo">
          <label for="ptn">Proteínas (%)</label>
          <input id="ptn" v-model.number="form.proteinasPercentual" type="number" step="0.5"
                 min="0" max="100" :class="{ atencao: foraDaFaixa(form.proteinasPercentual, 'proteinas') }"
                 @change="calcularPrevia" />
          <span v-if="faixas" class="ajuda">{{ faixas.proteinas.minimo }}–{{ faixas.proteinas.maximo }}%</span>
        </div>
        <div class="campo">
          <label for="lip">Lipídios (%)</label>
          <input id="lip" v-model.number="form.lipidiosPercentual" type="number" step="0.5"
                 min="0" max="100" :class="{ atencao: foraDaFaixa(form.lipidiosPercentual, 'lipidios') }"
                 @change="calcularPrevia" />
          <span v-if="faixas" class="ajuda">{{ faixas.lipidios.minimo }}–{{ faixas.lipidios.maximo }}%</span>
        </div>
        <div class="soma" :class="{ invalida: !somaValida }">
          <span class="rotulo">Soma</span>
          <strong class="numerico">{{ soma }}%</strong>
        </div>
      </div>

      <!-- RN15 — a soma precisa fechar em 100% -->
      <p v-if="!somaValida" class="erro-campo">
        A soma dos percentuais deve ser exatamente 100%.
      </p>

      <p v-else-if="algumForaDaFaixa" class="aviso aviso-atencao fora-faixa">
        A distribuição está fora das faixas da diretriz. O plano pode ser salvo assim —
        a conduta é sua.
      </p>

      <div v-if="previa" class="previa">
        <span>VET <strong class="numerico">{{ previa.totalCaloriasPrescritas }}</strong> kcal/dia</span>
        <span>CHO <strong class="numerico">{{ previa.carboidratosGramas }}</strong> g</span>
        <span>PTN <strong class="numerico">{{ previa.proteinasGramas }}</strong> g</span>
        <span>LIP <strong class="numerico">{{ previa.lipidiosGramas }}</strong> g</span>
      </div>

      <p class="rotulo-secao">Refeições</p>
      <div class="seletor">
        <select v-model="refeicaoAtual" class="refeicao">
          <option v-for="r in REFEICOES" :key="r" :value="r">{{ r }}</option>
        </select>

        <div class="autocomplete">
          <input v-model="busca" placeholder="Buscar alimento…" />
          <ul v-if="resultados.length && !selecionado" class="sugestoes">
            <li v-for="a in resultados" :key="a.id" @click="selecionado = a; busca = a.nome; resultados = []">
              {{ a.nome }}
              <span class="kcal numerico">{{ a.caloriasPor100g ?? '—' }} kcal/100g</span>
            </li>
          </ul>
        </div>

        <input v-model.number="quantidade" type="number" min="1" max="5000" class="qtd" placeholder="g" />
        <button class="btn btn-secundario pequeno" :disabled="!selecionado || !quantidade" @click="adicionarItem">
          Adicionar
        </button>
      </div>

      <div v-if="itens.length === 0" class="vazio-itens">
        Nenhum alimento adicionado. O plano pode ser salvo sem refeições e completado depois.
      </div>

      <div v-for="[refeicao, lista] in porRefeicao" :key="refeicao" class="grupo-refeicao">
        <h4>{{ refeicao }}</h4>
        <ul>
          <li v-for="(item, i) in lista" :key="i">
            <span class="nome">{{ item.nome }}</span>
            <span class="numerico">{{ item.quantidade }} g</span>
            <span v-if="caloriasDoItem(item)" class="numerico kcal">{{ caloriasDoItem(item) }} kcal</span>
            <button class="remover" @click="itens.splice(itens.indexOf(item), 1)">remover</button>
          </li>
        </ul>
      </div>

      <div class="acoes">
        <button class="btn btn-primario pequeno" :disabled="salvando || !form.objetivo || !somaValida"
                @click="salvarPlano">
          {{ salvando ? 'Salvando…' : 'Salvar e ativar plano' }}
        </button>
        <button class="btn btn-secundario pequeno" @click="montando = false">Cancelar</button>
      </div>
      <p class="aviso-substituicao">
        Salvar um novo plano inativa o plano vigente, que continua disponível no histórico.
      </p>
    </template>

    <!-- ── Plano vigente ── -->
    <template v-else-if="plano">
      <div class="cabecalho-plano">
        <div>
          <strong class="objetivo">{{ plano.objetivo }}</strong>
          <span class="desde numerico">desde {{ data(plano.dataInicio) }}</span>
        </div>
        <span class="selo selo-ok">Ativo</span>
      </div>

      <div v-if="plano.distribuicao" class="distribuicao">
        <div>
          <span class="rotulo">VET prescrito</span>
          <strong class="numerico grande">{{ plano.totalCaloriasPrescritas }}</strong>
          <span class="unidade">kcal/dia</span>
        </div>
        <div>
          <span class="rotulo">Carboidratos</span>
          <strong class="numerico">{{ plano.distribuicao.carboidratosPercentual }}%</strong>
          <span class="unidade numerico">{{ plano.distribuicao.carboidratosGramas }} g</span>
        </div>
        <div>
          <span class="rotulo">Proteínas</span>
          <strong class="numerico">{{ plano.distribuicao.proteinasPercentual }}%</strong>
          <span class="unidade numerico">{{ plano.distribuicao.proteinasGramas }} g</span>
        </div>
        <div>
          <span class="rotulo">Lipídios</span>
          <strong class="numerico">{{ plano.distribuicao.lipidiosPercentual }}%</strong>
          <span class="unidade numerico">{{ plano.distribuicao.lipidiosGramas }} g</span>
        </div>
      </div>

      <p v-if="plano.itens.length === 0" class="vazio-itens">
        As refeições ainda não foram montadas neste plano.
      </p>

      <template v-else>
        <table class="tabela">
          <thead>
            <tr><th>Refeição</th><th>Calorias</th><th>CHO</th><th>PTN</th><th>LIP</th></tr>
          </thead>
          <tbody>
            <tr v-for="t in plano.totaisPorRefeicao" :key="t.refeicao">
              <td>{{ t.refeicao }}</td>
              <td class="numerico">{{ t.calorias }} kcal</td>
              <td class="numerico">{{ t.carboidratos }} g</td>
              <td class="numerico">{{ t.proteinas }} g</td>
              <td class="numerico">{{ t.lipidios }} g</td>
            </tr>
          </tbody>
        </table>

        <!-- UC007 passo 7 — desvio em relação ao VET -->
        <p class="fechamento" :class="plano.dentroDaMargem ? 'ok' : 'atencao'">
          Montado: <strong class="numerico">{{ plano.caloriasMontadas }}</strong> kcal
          <template v-if="plano.desvioPercentualDoVet != null">
            · desvio de <strong class="numerico">{{ plano.desvioPercentualDoVet }}%</strong>
            {{ plano.dentroDaMargem ? 'em relação ao VET (dentro da margem de ±5%)' : 'em relação ao VET (fora da margem de ±5%)' }}
          </template>
        </p>

        <details class="itens-detalhe">
          <summary>{{ plano.itens.length }} item(ns) do plano</summary>
          <ul class="lista-itens">
            <li v-for="i in plano.itens" :key="i.id">
              <span class="refeicao-tag">{{ i.refeicao || '—' }}</span>
              <span class="nome">{{ i.alimentoNome }}</span>
              <span class="numerico">{{ i.quantidade }} {{ i.unidade }}</span>
              <span class="numerico kcal">{{ i.calorias ?? '—' }} kcal</span>
            </li>
          </ul>
        </details>
      </template>
    </template>

    <p v-else class="vazio">
      Nenhum plano alimentar ativo. Calcule a necessidade energética e elabore o primeiro plano.
    </p>

    <!-- RN17 — histórico preservado -->
    <details v-if="historico.filter(p => !p.ativo).length" class="historico">
      <summary>{{ historico.filter(p => !p.ativo).length }} plano(s) no histórico</summary>
      <ul>
        <li v-for="p in historico.filter(x => !x.ativo)" :key="p.id">
          <span class="objetivo-hist">{{ p.objetivo }}</span>
          <span class="numerico desde">{{ data(p.dataInicio) }}</span>
          <span class="numerico">{{ p.itens.length }} itens</span>
          <button class="btn btn-secundario" @click="reativar(p.id)">Reativar</button>
        </li>
      </ul>
    </details>
  </section>
</template>

<style scoped>
.topo { margin-bottom: var(--md); }
.card h3 { font-size: 15px; color: var(--text-secondary); }
.btn.pequeno { padding: 6px 14px; font-size: 14px; }
.rotulo { font-size: 11px; letter-spacing: 0.05em; text-transform: uppercase; color: var(--text-muted); }
.rotulo-secao { font-size: 13px; color: var(--text-muted); margin: var(--lg) 0 var(--xs); }

.macros { display: grid; grid-template-columns: repeat(4, 1fr); gap: var(--md); align-items: end; }
@media (max-width: 720px) { .macros { grid-template-columns: 1fr 1fr; } }
.soma { display: flex; flex-direction: column; padding-bottom: var(--md); }
.soma strong { font-size: 20px; color: var(--success); }
.soma.invalida strong { color: var(--danger); }

.previa {
  display: flex; gap: var(--lg); flex-wrap: wrap;
  padding: var(--sm) var(--md); background: var(--teal-light);
  border-radius: var(--raio); font-size: 14px; margin-top: var(--xs);
}

.seletor { display: flex; gap: var(--xs); align-items: flex-start; flex-wrap: wrap; }
.seletor select, .seletor input {
  font-family: inherit; font-size: 14px; padding: 8px 10px;
  border: 1px solid var(--border); border-radius: var(--raio); background: var(--background);
}
.refeicao { min-width: 150px; }
.qtd { width: 80px; }
.autocomplete { position: relative; flex: 1; min-width: 200px; }
.autocomplete input { width: 100%; }
.sugestoes {
  position: absolute; z-index: 10; top: 100%; left: 0; right: 0;
  list-style: none; margin: 2px 0 0; padding: 4px;
  background: var(--surface); border: 1px solid var(--border);
  border-radius: var(--raio); box-shadow: 0 1px 3px rgba(15, 23, 42, 0.08);
  max-height: 240px; overflow-y: auto;
}
.sugestoes li {
  padding: 7px 10px; border-radius: 6px; cursor: pointer; font-size: 14px;
  display: flex; justify-content: space-between; gap: var(--sm);
}
.sugestoes li:hover { background: var(--hover); }
.kcal { color: var(--text-muted); font-size: 12px; }

.grupo-refeicao { margin-top: var(--md); }
.grupo-refeicao h4 { font-size: 13px; margin: 0 0 4px; color: var(--text-secondary); }
.grupo-refeicao ul { list-style: none; margin: 0; padding: 0; }
.grupo-refeicao li {
  display: flex; align-items: center; gap: var(--sm);
  padding: 5px 0; border-bottom: 1px solid var(--border); font-size: 14px;
}
.nome { flex: 1; }
.remover {
  background: none; border: none; color: var(--danger);
  font-family: inherit; font-size: 12px; cursor: pointer; padding: 0;
}

.vazio-itens { color: var(--text-muted); font-size: 14px; padding: var(--md) 0; }
.acoes { display: flex; gap: var(--xs); margin-top: var(--lg); }
.aviso-substituicao { font-size: 12px; color: var(--text-muted); margin: var(--xs) 0 0; }

.cabecalho-plano { display: flex; align-items: center; justify-content: space-between; margin-bottom: var(--md); }
.objetivo { font-size: 16px; }
.desde { display: block; font-size: 12px; color: var(--text-muted); }

.distribuicao {
  display: grid; grid-template-columns: repeat(4, 1fr); gap: var(--md);
  padding: var(--md); background: var(--background);
  border-radius: var(--raio); margin-bottom: var(--md);
}
@media (max-width: 720px) { .distribuicao { grid-template-columns: 1fr 1fr; } }
.distribuicao > div { display: flex; flex-direction: column; }
.distribuicao strong { font-size: 18px; }
.grande { font-size: 24px; color: var(--primary); }
.unidade { font-size: 12px; color: var(--text-secondary); }

.fechamento { font-size: 14px; margin: var(--md) 0 0; }
.fechamento.ok { color: var(--text-secondary); }
.fechamento.atencao { color: var(--warning); }

.itens-detalhe, .historico { margin-top: var(--md); }
.itens-detalhe summary, .historico summary { font-size: 13px; color: var(--text-muted); cursor: pointer; }
.lista-itens { list-style: none; margin: var(--sm) 0 0; padding: 0; }
.lista-itens li {
  display: flex; align-items: center; gap: var(--sm);
  padding: 5px 0; border-bottom: 1px solid var(--border); font-size: 14px;
}
.refeicao-tag { font-size: 12px; color: var(--text-muted); min-width: 110px; }

.historico ul { list-style: none; margin: var(--sm) 0 0; padding: 0; }
.historico li {
  display: flex; align-items: center; gap: var(--sm);
  padding: 6px 0; border-bottom: 1px solid var(--border); font-size: 14px;
}
.objetivo-hist { flex: 1; }
.historico .btn { padding: 3px 10px; font-size: 12px; }

.cabecalho-macros { align-items: baseline; }
.fonte-faixa { font-size: 12px; color: var(--text-muted); }
.macros input.atencao { border-color: var(--warning); }
.fora-faixa { margin-top: var(--xs); font-size: 13px; }
</style>
