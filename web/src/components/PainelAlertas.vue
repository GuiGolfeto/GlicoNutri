<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type { Alerta, CriarAlerta, HistoricoAlerta, Referencia } from '../api/tipos'

const props = defineProps<{ pacienteId: number }>()

const alertas = ref<Alerta[]>([])
const disparos = ref<HistoricoAlerta[]>([])
const tipos = ref<Referencia[]>([])

const carregando = ref(true)
const erro = ref('')
const sucesso = ref('')
const abrindoForm = ref(false)
const editando = ref<number | null>(null)
const enviando = ref(false)

const DIAS = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S']

const vazio = (): CriarAlerta => ({
  tipoId: null, mensagem: '', horario1: '08:00', horario2: null, diasSemana: '1111111',
})

const form = reactive<CriarAlerta>(vazio())

async function carregar() {
  carregando.value = true
  try {
    const [a, h] = await Promise.all([
      api.get<Alerta[]>(`/pacientes/${props.pacienteId}/alertas`),
      api.get<HistoricoAlerta[]>(`/pacientes/${props.pacienteId}/alertas/disparos`, { params: { dias: 30 } }),
    ])
    alertas.value = a.data
    disparos.value = h.data
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar os alertas.')
  } finally {
    carregando.value = false
  }
}

onMounted(async () => {
  const { data } = await api.get<Referencia[]>('/referencias/tipos-alerta')
  tipos.value = data
  await carregar()
})

function abrirNovo() {
  Object.assign(form, vazio())
  form.tipoId = tipos.value[0]?.id ?? null
  editando.value = null
  abrindoForm.value = true
}

function abrirEdicao(a: Alerta) {
  Object.assign(form, {
    tipoId: a.tipoId,
    mensagem: a.mensagem,
    horario1: (a.horario1 ?? '08:00:00').slice(0, 5),
    horario2: a.horario2 ? a.horario2.slice(0, 5) : null,
    diasSemana: a.diasSemana ?? '1111111',
  })
  editando.value = a.id
  abrindoForm.value = true
}

/** Máscara de 7 posições começando no domingo — o formato que cabe no banco. */
function alternarDia(indice: number) {
  const atual = (form.diasSemana ?? '1111111').split('')
  atual[indice] = atual[indice] === '1' ? '0' : '1'
  form.diasSemana = atual.join('')
}

const diaAtivo = (i: number) => (form.diasSemana ?? '1111111')[i] === '1'

async function salvar() {
  erro.value = ''
  sucesso.value = ''
  enviando.value = true

  const corpo = {
    ...form,
    horario1: `${form.horario1}:00`,
    horario2: form.horario2 ? `${form.horario2}:00` : null,
  }

  try {
    if (editando.value) await api.put(`/pacientes/${props.pacienteId}/alertas/${editando.value}`, corpo)
    else await api.post(`/pacientes/${props.pacienteId}/alertas`, corpo)

    abrindoForm.value = false
    await carregar()
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}

async function alternarSituacao(a: Alerta) {
  erro.value = ''
  try {
    if (a.ativo) await api.delete(`/pacientes/${props.pacienteId}/alertas/${a.id}`)
    else await api.post(`/pacientes/${props.pacienteId}/alertas/${a.id}/reativar`)
    await carregar()
  } catch (e) {
    erro.value = mensagemDeErro(e)
  }
}

/** RN35 — varredura que expira pendentes com mais de duas horas. */
async function verificarExpirados() {
  erro.value = ''
  try {
    const { data } = await api.post<{ expirados: number; pendentesRestantes: number }>(
      `/pacientes/${props.pacienteId}/alertas/verificar-expirados`, {})
    sucesso.value = data.expirados > 0
      ? `${data.expirados} alerta(s) expirado(s) por falta de resposta; ${data.pendentesRestantes} ainda pendente(s).`
      : 'Nenhum alerta pendente fora da janela de 2 horas.'
    await carregar()
  } catch (e) {
    erro.value = mensagemDeErro(e)
  }
}

const seloStatus = (s: string) =>
  s === 'ATENDIDO' ? 'selo-ok' : s === 'PENDENTE' ? 'selo-alerta' : s === 'FALHOU' ? 'selo-perigo' : 'selo-neutro'

const horaCurta = (h: string | null) => (h ? h.slice(0, 5) : null)

const quando = (iso: string) =>
  new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })

const resumoDias = (mascara: string | null) => {
  if (!mascara || mascara === '1111111') return 'todos os dias'
  return mascara.split('').map((v, i) => (v === '1' ? DIAS[i] : null)).filter(Boolean).join(' ')
}
</script>

<template>
  <section class="card">
    <div class="entre topo">
      <h3>Alertas e lembretes</h3>
      <div class="linha">
        <button class="btn btn-secundario pequeno" @click="verificarExpirados">Verificar expirados</button>
        <button v-if="!abrindoForm" class="btn btn-secundario pequeno" @click="abrirNovo">Novo alerta</button>
      </div>
    </div>

    <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>
    <div v-if="sucesso" class="aviso aviso-sucesso">{{ sucesso }}</div>

    <form v-if="abrindoForm" class="formulario" @submit.prevent="salvar">
      <div class="grade-2">
        <div class="campo">
          <label for="tipo">Tipo *</label>
          <select id="tipo" v-model="form.tipoId" required>
            <option v-for="t in tipos" :key="t.id" :value="t.id">{{ t.descricao }}</option>
          </select>
        </div>
        <div class="campo">
          <label for="msg">Mensagem da notificação</label>
          <input id="msg" v-model="form.mensagem" maxlength="500"
                 placeholder="Ex.: hora de registrar sua glicemia" />
        </div>
      </div>

      <div class="grade-2">
        <div class="campo">
          <label for="h1">Primeiro horário *</label>
          <input id="h1" v-model="form.horario1" type="time" required />
        </div>
        <div class="campo">
          <label for="h2">Segundo horário</label>
          <input id="h2" v-model="form.horario2" type="time" />
        </div>
      </div>

      <div class="campo">
        <label>Dias da semana</label>
        <div class="dias">
          <button v-for="(d, i) in DIAS" :key="i" type="button" class="dia"
                  :class="{ ativo: diaAtivo(i) }" @click="alternarDia(i)">{{ d }}</button>
        </div>
      </div>

      <div class="acoes">
        <button class="btn btn-primario pequeno" type="submit" :disabled="enviando">
          {{ enviando ? 'Salvando…' : 'Salvar alerta' }}
        </button>
        <button class="btn btn-secundario pequeno" type="button" @click="abrindoForm = false">Cancelar</button>
      </div>
    </form>

    <p v-if="carregando" class="vazio">Carregando…</p>
    <p v-else-if="alertas.length === 0" class="vazio">Nenhum alerta configurado.</p>

    <table v-else class="tabela">
      <thead>
        <tr><th>Tipo</th><th>Horários</th><th>Dias</th><th>Mensagem</th><th>Pendentes</th><th></th></tr>
      </thead>
      <tbody>
        <tr v-for="a in alertas" :key="a.id" :class="{ inativo: !a.ativo }">
          <td>{{ a.tipo }}</td>
          <td class="numerico">
            {{ horaCurta(a.horario1) }}<template v-if="a.horario2"> e {{ horaCurta(a.horario2) }}</template>
          </td>
          <td class="dias-resumo">{{ resumoDias(a.diasSemana) }}</td>
          <td class="mensagem">{{ a.mensagem || '—' }}</td>
          <td class="numerico">
            <span v-if="a.disparosPendentes" class="selo selo-alerta">{{ a.disparosPendentes }}</span>
            <span v-else>—</span>
          </td>
          <td class="acao">
            <button class="btn btn-secundario" @click="abrirEdicao(a)">Editar</button>
            <button class="btn" :class="a.ativo ? 'btn-perigo' : 'btn-secundario'" @click="alternarSituacao(a)">
              {{ a.ativo ? 'Desativar' : 'Reativar' }}
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <details v-if="disparos.length" class="historico">
      <summary>Histórico de disparos ({{ disparos.length }})</summary>
      <table class="tabela">
        <thead>
          <tr><th>Disparo</th><th>Tipo</th><th>Status</th><th>Atendido em</th><th>Tentativas</th><th>Causa</th></tr>
        </thead>
        <tbody>
          <tr v-for="d in disparos" :key="d.id">
            <td class="numerico">{{ quando(d.dataHoraDisparo) }}</td>
            <td>{{ d.tipoAlerta }}</td>
            <td><span class="selo" :class="seloStatus(d.status)">{{ d.status }}</span></td>
            <td class="numerico">{{ d.dataHoraAtendimento ? quando(d.dataHoraAtendimento) : '—' }}</td>
            <td class="numerico">{{ d.tentativas }}</td>
            <td class="causa">{{ d.mensagemErro || '—' }}</td>
          </tr>
        </tbody>
      </table>
    </details>
  </section>
</template>

<style scoped>
.topo { margin-bottom: var(--md); }
.card h3 { font-size: 15px; color: var(--text-secondary); }
.btn.pequeno { padding: 6px 14px; font-size: 14px; }

.formulario { padding: var(--md); background: var(--background); border: 1px solid var(--border); border-radius: var(--raio); margin-bottom: var(--md); }
.acoes { display: flex; gap: var(--xs); }

.dias { display: flex; gap: 4px; }
.dia {
  width: 34px; height: 34px; border-radius: var(--raio);
  border: 1px solid var(--border); background: var(--surface);
  font-family: inherit; font-size: 13px; cursor: pointer; color: var(--text-secondary);
}
.dia.ativo { background: var(--teal-light); border-color: var(--primary); color: var(--primary); font-weight: 600; }

.inativo { opacity: 0.55; }
.dias-resumo, .mensagem, .causa { font-size: 13px; color: var(--text-secondary); }
.acao { text-align: right; white-space: nowrap; }
.acao .btn { padding: 4px 10px; font-size: 13px; margin-left: 4px; }

.historico { margin-top: var(--md); }
.historico summary { font-size: 13px; color: var(--text-muted); cursor: pointer; margin-bottom: var(--sm); }
</style>
