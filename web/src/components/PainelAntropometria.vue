<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type { CriarRegistroAntropometrico, RegistroAntropometrico } from '../api/tipos'

const props = defineProps<{ pacienteId: number }>()
const emit = defineEmits<{ registrado: [] }>()

const historico = ref<RegistroAntropometrico[]>([])
const carregando = ref(true)
const erro = ref('')
const abrindoForm = ref(false)
const enviando = ref(false)

const form = reactive<CriarRegistroAntropometrico>({
  peso: null, altura: null,
  circunferenciaCintura: null, circunferenciaQuadril: null,
  circunferenciaAbdominal: null, circunferenciaBraco: null,
})

const ultimo = computed(() => historico.value[0] ?? null)

/** Pré-visualiza o IMC com a mesma fórmula da RN21, antes de salvar. */
const imcPrevia = computed(() => {
  if (!form.peso || !form.altura || form.altura < 50) return null
  const m = form.altura / 100
  const imc = form.peso / (m * m)
  const classificacao =
    imc < 18.5 ? 'Abaixo do peso' : imc < 25 ? 'Peso normal' : imc < 30 ? 'Sobrepeso' : 'Obesidade'
  return { imc: imc.toFixed(2), classificacao }
})

async function carregar() {
  carregando.value = true
  try {
    const { data } = await api.get<RegistroAntropometrico[]>(`/pacientes/${props.pacienteId}/antropometria`)
    historico.value = data
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar as medidas.')
  } finally {
    carregando.value = false
  }
}

onMounted(carregar)

async function salvar() {
  erro.value = ''
  enviando.value = true
  try {
    await api.post(`/pacientes/${props.pacienteId}/antropometria`, form)
    abrindoForm.value = false
    Object.assign(form, {
      peso: null, altura: null, circunferenciaCintura: null,
      circunferenciaQuadril: null, circunferenciaAbdominal: null, circunferenciaBraco: null,
    })
    await carregar()
    // O VET depende da medição mais recente (RN03 do UC008).
    emit('registrado')
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}

function abrir() {
  // Altura costuma não mudar entre consultas: repetir a última poupa digitação
  // e evita erro num campo que entra ao quadrado no IMC.
  if (ultimo.value) form.altura = ultimo.value.altura
  abrindoForm.value = true
}

const seloImc = (c: string | null) =>
  c === 'Peso normal' ? 'selo-ok' : c === 'Sobrepeso' ? 'selo-alerta' : c ? 'selo-perigo' : 'selo-neutro'

const sinal = (v: number | null) => (v === null ? '' : v > 0 ? `+${v}` : `${v}`)
const data = (iso: string) => new Date(iso).toLocaleDateString('pt-BR')
</script>

<template>
  <section class="card">
    <div class="entre topo">
      <h3>Dados antropométricos</h3>
      <button v-if="!abrindoForm" class="btn btn-secundario pequeno" @click="abrir">Nova medição</button>
    </div>

    <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>

    <form v-if="abrindoForm" class="formulario" @submit.prevent="salvar">
      <div class="grade-2">
        <div class="campo">
          <label for="peso">Peso (kg) *</label>
          <input id="peso" v-model.number="form.peso" type="number" step="0.1" min="1" max="500" required />
        </div>
        <div class="campo">
          <label for="altura">Altura (cm) *</label>
          <input id="altura" v-model.number="form.altura" type="number" step="0.5" min="50" max="250" required />
          <span class="ajuda">Em centímetros — ex.: 168</span>
        </div>
      </div>

      <!-- UC008 A2 — o IMC é sempre do sistema, nunca digitado. -->
      <div v-if="imcPrevia" class="previa">
        IMC calculado: <strong class="numerico">{{ imcPrevia.imc }}</strong>
        <span class="selo" :class="seloImc(imcPrevia.classificacao)">{{ imcPrevia.classificacao }}</span>
      </div>

      <p class="rotulo">Circunferências (opcionais)</p>
      <div class="grade-2">
        <div class="campo">
          <label for="cintura">Cintura (cm)</label>
          <input id="cintura" v-model.number="form.circunferenciaCintura" type="number" step="0.5" />
        </div>
        <div class="campo">
          <label for="quadril">Quadril (cm)</label>
          <input id="quadril" v-model.number="form.circunferenciaQuadril" type="number" step="0.5" />
          <span class="ajuda">Com a cintura, o sistema calcula a RCQ.</span>
        </div>
      </div>
      <div class="grade-2">
        <div class="campo">
          <label for="abdominal">Abdominal (cm)</label>
          <input id="abdominal" v-model.number="form.circunferenciaAbdominal" type="number" step="0.5" />
        </div>
        <div class="campo">
          <label for="braco">Braço (cm)</label>
          <input id="braco" v-model.number="form.circunferenciaBraco" type="number" step="0.5" />
        </div>
      </div>

      <div class="acoes">
        <button class="btn btn-primario pequeno" type="submit" :disabled="enviando">
          {{ enviando ? 'Salvando…' : 'Salvar medição' }}
        </button>
        <button class="btn btn-secundario pequeno" type="button" @click="abrindoForm = false">Cancelar</button>
      </div>
    </form>

    <p v-if="carregando" class="vazio">Carregando…</p>
    <p v-else-if="historico.length === 0 && !abrindoForm" class="vazio">
      Nenhuma medição registrada. É o primeiro passo antes do cálculo energético.
    </p>

    <table v-else-if="historico.length" class="tabela">
      <thead>
        <tr><th>Data</th><th>Peso</th><th>IMC</th><th>RCQ</th><th>Variação</th></tr>
      </thead>
      <tbody>
        <tr v-for="r in historico" :key="r.id">
          <td class="numerico">{{ data(r.dataHora) }}</td>
          <td class="numerico">{{ r.peso }} kg</td>
          <td>
            <span class="numerico">{{ r.imc }}</span>
            <span class="selo" :class="seloImc(r.classificacaoImc)">{{ r.classificacaoImc }}</span>
          </td>
          <td class="numerico">{{ r.rcq ?? '—' }}</td>
          <td class="numerico">
            <!-- UC008 A3 — delta em valor e percentual sobre a medição anterior. -->
            <span v-if="r.comparativoPeso?.delta != null"
                  :class="r.comparativoPeso.delta < 0 ? 'baixa' : 'alta'">
              {{ sinal(r.comparativoPeso.delta) }} kg
              ({{ sinal(r.comparativoPeso.deltaPercentual) }}%)
            </span>
            <span v-else class="primeiro">primeira</span>
          </td>
        </tr>
      </tbody>
    </table>
  </section>
</template>

<style scoped>
.topo { margin-bottom: var(--md); }
.card h3 { font-size: 15px; color: var(--text-secondary); }
.btn.pequeno { padding: 6px 14px; font-size: 14px; }

.formulario { padding: var(--md); background: var(--background); border: 1px solid var(--border); border-radius: var(--raio); margin-bottom: var(--md); }
.rotulo { font-size: 13px; color: var(--text-muted); margin: var(--xs) 0 var(--sm); }
.previa { font-size: 15px; margin-bottom: var(--md); display: flex; align-items: center; gap: var(--xs); }
.acoes { display: flex; gap: var(--xs); }

.tabela td .selo { margin-left: var(--xs); }
.baixa { color: var(--success); }
.alta { color: var(--warning); }
.primeiro { color: var(--text-muted); font-size: 13px; }
</style>
