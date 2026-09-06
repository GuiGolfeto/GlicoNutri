<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type { CalcularVet, NecessidadeEnergetica, Referencia } from '../api/tipos'

const props = defineProps<{ pacienteId: number; recarregar: number }>()
const emit = defineEmits<{ calculado: [] }>()

const historico = ref<NecessidadeEnergetica[]>([])
const formulas = ref<Referencia[]>([])
const niveis = ref<Referencia[]>([])

const carregando = ref(true)
const erro = ref('')
const bloqueio = ref('')
const simulando = ref(false)
const salvando = ref(false)

/** UC006, passo 6: o resultado aparece antes de o nutricionista confirmar. */
const previa = ref<NecessidadeEnergetica | null>(null)

const form = reactive<CalcularVet>({ formulaId: null, nivelAtividadeId: null, objetivo: '' })

const atual = computed(() => historico.value[0] ?? null)

async function carregar() {
  carregando.value = true
  try {
    const [f, n, h] = await Promise.all([
      api.get<Referencia[]>('/referencias/formulas-energeticas'),
      api.get<Referencia[]>('/referencias/niveis-atividade'),
      api.get<NecessidadeEnergetica[]>(`/pacientes/${props.pacienteId}/necessidade-energetica`),
    ])
    formulas.value = f.data
    niveis.value = n.data
    historico.value = h.data
    // RN02 do UC006 — Harris-Benedict é a fórmula padrão.
    form.formulaId ??= f.data[0]?.id ?? null
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar os cálculos.')
  } finally {
    carregando.value = false
  }
}

onMounted(carregar)

// Uma medição nova muda a base do cálculo (RN03 do UC008): a prévia em tela
// passa a estar desatualizada e é descartada.
watch(() => props.recarregar, () => { previa.value = null; bloqueio.value = ''; carregar() })

async function simular() {
  erro.value = ''
  bloqueio.value = ''
  previa.value = null
  simulando.value = true
  try {
    const { data } = await api.post<NecessidadeEnergetica>(
      `/pacientes/${props.pacienteId}/necessidade-energetica/simular`, form)
    previa.value = data
  } catch (e) {
    // UC006 A2 — sem antropometria o cálculo é bloqueado e o usuário é mandado
    // ao registro de peso e altura.
    const msg = mensagemDeErro(e)
    if (msg.includes('antropométrico')) bloqueio.value = msg
    else erro.value = msg
  } finally {
    simulando.value = false
  }
}

async function confirmar() {
  erro.value = ''
  salvando.value = true
  try {
    await api.post(`/pacientes/${props.pacienteId}/necessidade-energetica`, form)
    previa.value = null
    await carregar()
    emit('calculado')
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    salvando.value = false
  }
}

const data = (iso: string) => new Date(iso).toLocaleDateString('pt-BR')
const sinal = (v: number | null) => (v === null ? '' : v > 0 ? `+${v}` : `${v}`)
</script>

<template>
  <section class="card">
    <h3>Necessidade energética</h3>

    <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>

    <!-- UC006 A2 — pré-requisito não atendido. -->
    <div v-if="bloqueio" class="aviso aviso-atencao">{{ bloqueio }}</div>

    <div v-if="atual" class="vet-atual">
      <span class="rotulo">VET vigente</span>
      <strong class="numerico grande">{{ atual.valorKcal }}</strong>
      <span class="unidade">kcal/dia</span>
      <span class="detalhe">{{ atual.formula }} · {{ atual.nivelAtividade }}</span>
    </div>

    <form v-if="!carregando" class="formulario" @submit.prevent="simular">
      <div class="grade-2">
        <div class="campo">
          <label for="formula">Fórmula</label>
          <select id="formula" v-model="form.formulaId" required>
            <option v-for="f in formulas" :key="f.id" :value="f.id">{{ f.descricao }}</option>
          </select>
        </div>
        <div class="campo">
          <label for="nivel">Fator de atividade</label>
          <select id="nivel" v-model="form.nivelAtividadeId" required>
            <option :value="null" disabled>Selecione…</option>
            <option v-for="n in niveis" :key="n.id" :value="n.id">{{ n.descricao }}</option>
          </select>
        </div>
      </div>

      <div class="campo">
        <label for="objetivo">Objetivo</label>
        <input id="objetivo" v-model="form.objetivo" placeholder="Ex.: manutenção de peso" />
      </div>

      <button class="btn btn-secundario pequeno" type="submit" :disabled="simulando || !form.nivelAtividadeId">
        {{ simulando ? 'Calculando…' : 'Calcular' }}
      </button>
    </form>

    <!-- UC006 A4 — o resultado expõe TMB, fator e VET, não só o número final. -->
    <div v-if="previa" class="resultado">
      <div class="conta">
        <div class="parcela">
          <span class="rotulo">TMB</span>
          <strong class="numerico">{{ previa.tmb }}</strong>
          <span class="unidade">kcal</span>
        </div>
        <span class="operador">×</span>
        <div class="parcela">
          <span class="rotulo">Fator</span>
          <strong class="numerico">{{ previa.fatorAtividade }}</strong>
          <span class="unidade">{{ previa.nivelAtividade }}</span>
        </div>
        <span class="operador">=</span>
        <div class="parcela destaque">
          <span class="rotulo">VET</span>
          <strong class="numerico">{{ previa.valorKcal }}</strong>
          <span class="unidade">kcal/dia</span>
        </div>
      </div>

      <p class="base">
        Calculado sobre a medição de {{ data(previa.dataMedicaoUtilizada!) }}:
        <strong class="numerico">{{ previa.pesoUtilizado }} kg</strong>,
        <strong class="numerico">{{ previa.alturaUtilizada }} cm</strong>,
        <strong class="numerico">{{ previa.idadeUtilizada }} anos</strong>,
        {{ previa.sexoUtilizado }}.
      </p>

      <!-- UC006 A3 — comparação com o cálculo anterior. -->
      <p v-if="previa.vetAnterior" class="base">
        Cálculo anterior: <strong class="numerico">{{ previa.vetAnterior }}</strong> kcal/dia
        (<span :class="previa.variacaoPercentual! > 0 ? 'alta' : 'baixa'">
          {{ sinal(previa.variacaoPercentual) }}%
        </span>)
      </p>

      <div class="acoes">
        <button class="btn btn-primario pequeno" :disabled="salvando" @click="confirmar">
          {{ salvando ? 'Salvando…' : 'Confirmar e salvar no prontuário' }}
        </button>
        <button class="btn btn-secundario pequeno" @click="previa = null">Descartar</button>
      </div>
    </div>

    <template v-if="historico.length">
      <p class="rotulo secao">Histórico de cálculos</p>
      <table class="tabela">
        <thead><tr><th>Data</th><th>Fórmula</th><th>Fator</th><th>VET</th><th>Objetivo</th></tr></thead>
        <tbody>
          <tr v-for="n in historico" :key="n.id!">
            <td class="numerico">{{ data(n.dataCalculo) }}</td>
            <td>{{ n.formula }}</td>
            <td class="numerico">{{ n.nivelAtividade }} ({{ n.fatorAtividade }})</td>
            <td class="numerico">{{ n.valorKcal }} kcal</td>
            <td>{{ n.objetivo || '—' }}</td>
          </tr>
        </tbody>
      </table>
    </template>
  </section>
</template>

<style scoped>
.card h3 { font-size: 15px; color: var(--text-secondary); margin-bottom: var(--md); }
.btn.pequeno { padding: 6px 14px; font-size: 14px; }
.rotulo { font-size: 12px; letter-spacing: 0.05em; text-transform: uppercase; color: var(--text-muted); }

.vet-atual {
  display: flex; align-items: baseline; gap: var(--xs); flex-wrap: wrap;
  padding: var(--sm) var(--md); margin-bottom: var(--md);
  background: var(--teal-light); border-radius: var(--raio);
}
.grande { font-size: 28px; color: var(--primary); }
.unidade { font-size: 13px; color: var(--text-secondary); }
.vet-atual .detalhe { margin-left: auto; font-size: 13px; color: var(--text-secondary); }

.formulario { padding: var(--md); background: var(--background); border: 1px solid var(--border); border-radius: var(--raio); }

.resultado { margin-top: var(--md); padding: var(--md); border: 1px solid var(--primary); border-radius: var(--raio); }
.conta { display: flex; align-items: center; gap: var(--md); flex-wrap: wrap; }
.parcela { display: flex; flex-direction: column; }
.parcela strong { font-size: 20px; }
.parcela.destaque strong { font-size: 28px; color: var(--primary); }
.operador { font-size: 20px; color: var(--text-muted); }
.base { font-size: 13px; color: var(--text-secondary); margin: var(--sm) 0 0; }
.acoes { display: flex; gap: var(--xs); margin-top: var(--md); }

.secao { margin: var(--lg) 0 var(--xs); }
.baixa { color: var(--success); }
.alta { color: var(--warning); }
</style>
