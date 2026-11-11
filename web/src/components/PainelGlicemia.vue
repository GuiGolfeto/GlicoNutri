<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type { HistoricoGlicemico, Referencia, SerieGlicemica } from '../api/tipos'
import GraficoLinha, { type PontoGrafico } from './GraficoLinha.vue'

const props = defineProps<{ pacienteId: number }>()
const emit = defineEmits<{ registrado: [] }>()

const historico = ref<HistoricoGlicemico | null>(null)
const serie = ref<SerieGlicemica | null>(null)
const contextos = ref<Referencia[]>([])

const dias = ref(7)
const carregando = ref(true)
const erro = ref('')
const abrindoForm = ref(false)
const enviando = ref(false)

const form = reactive({
  valor: null as number | null,
  contextoId: null as number | null,
  dataHora: '',
  observacao: '',
})

const periodos = [7, 15, 30]

async function carregar() {
  carregando.value = true
  try {
    const [h, s] = await Promise.all([
      api.get<HistoricoGlicemico>(`/pacientes/${props.pacienteId}/glicemia`, { params: { dias: dias.value } }),
      api.get<SerieGlicemica>(`/pacientes/${props.pacienteId}/glicemia/serie`, { params: { dias: dias.value } }),
    ])
    historico.value = h.data
    serie.value = s.data
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar os registros.')
  } finally {
    carregando.value = false
  }
}

onMounted(async () => {
  const { data } = await api.get<Referencia[]>('/referencias/contextos-glicemia')
  contextos.value = data
  form.contextoId = data[0]?.id ?? null
  await carregar()
})

function trocarPeriodo(d: number) {
  dias.value = d
  carregar()
}

async function salvar() {
  erro.value = ''
  enviando.value = true
  try {
    await api.post(`/pacientes/${props.pacienteId}/glicemia`, {
      valor: form.valor,
      contextoId: form.contextoId,
      dataHora: form.dataHora ? new Date(form.dataHora).toISOString() : null,
      observacao: form.observacao || null,
    })
    abrindoForm.value = false
    form.valor = null
    form.dataHora = ''
    form.observacao = ''
    await carregar()
    emit('registrado')
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}

/**
 * Soft delete: o registro sai do histórico e das médias, mas continua no banco.
 * Existe porque um valor digitado errado — 1050 em vez de 105 — distorce a média
 * e o percentual no alvo até ser removido.
 */
async function remover(id: number) {
  erro.value = ''
  try {
    await api.delete(`/pacientes/${props.pacienteId}/glicemia/${id}`)
    await carregar()
    emit('registrado')
  } catch (e) {
    erro.value = mensagemDeErro(e)
  }
}

const pontos = computed<PontoGrafico[]>(() =>
  serie.value?.pontos.map((p) => ({
    data: p.dataHora, valor: p.valor, destaque: p.foraDoAlvo, rotulo: p.contexto,
  })) ?? [])

/** Classificação clínica absoluta, distinta da faixa alvo individual. */
const seloClassificacao = (c: string) =>
  c === 'NORMAL' ? 'selo-ok' : 'selo-perigo'

const textoClassificacao = (c: string) =>
  c === 'HIPOGLICEMIA' ? 'Hipoglicemia' : c === 'HIPERGLICEMIA' ? 'Hiperglicemia' : 'Normal'

const quando = (iso: string) =>
  new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })
</script>

<template>
  <section class="card">
    <div class="entre topo">
      <h3>Monitoramento glicêmico</h3>
      <div class="linha">
        <div class="periodos">
          <button v-for="d in periodos" :key="d" class="periodo"
                  :class="{ ativo: dias === d }" @click="trocarPeriodo(d)">{{ d }}d</button>
        </div>
        <button v-if="!abrindoForm" class="btn btn-secundario pequeno" @click="abrindoForm = true">
          Novo registro
        </button>
      </div>
    </div>

    <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>

    <form v-if="abrindoForm" class="formulario" @submit.prevent="salvar">
      <div class="grade-2">
        <div class="campo">
          <label for="valor">Glicemia (mg/dL) *</label>
          <input id="valor" v-model.number="form.valor" type="number" step="1" min="0" max="600" required />
        </div>
        <div class="campo">
          <label for="contexto">Contexto *</label>
          <select id="contexto" v-model="form.contextoId" required>
            <option v-for="c in contextos" :key="c.id" :value="c.id">{{ c.descricao }}</option>
          </select>
        </div>
      </div>
      <div class="grade-2">
        <div class="campo">
          <label for="quando">Data e hora</label>
          <input id="quando" v-model="form.dataHora" type="datetime-local" />
          <span class="ajuda">Em branco, usa o momento atual.</span>
        </div>
        <div class="campo">
          <label for="obs">Observação</label>
          <input id="obs" v-model="form.observacao" maxlength="500" placeholder="Ex.: senti tontura" />
        </div>
      </div>
      <div class="acoes">
        <button class="btn btn-primario pequeno" type="submit" :disabled="enviando || !form.valor">
          {{ enviando ? 'Salvando…' : 'Salvar registro' }}
        </button>
        <button class="btn btn-secundario pequeno" type="button" @click="abrindoForm = false">Cancelar</button>
      </div>
    </form>

    <p v-if="carregando" class="vazio">Carregando…</p>

    <template v-else-if="historico">
      <!-- Painel de estatísticas do UC005 -->
      <div class="indicadores">
        <div><span class="rotulo">Registros</span><strong class="numerico">{{ historico.resumo.totalRegistros }}</strong></div>
        <div><span class="rotulo">Média</span><strong class="numerico">{{ historico.resumo.media ?? '—' }}</strong></div>
        <div><span class="rotulo">Mínima</span><strong class="numerico">{{ historico.resumo.minimo ?? '—' }}</strong></div>
        <div><span class="rotulo">Máxima</span><strong class="numerico">{{ historico.resumo.maximo ?? '—' }}</strong></div>
        <div>
          <span class="rotulo">No alvo</span>
          <strong class="numerico" :class="(historico.resumo.percentualNoAlvo ?? 100) < 70 ? 'ruim' : 'bom'">
            {{ historico.resumo.percentualNoAlvo != null ? `${historico.resumo.percentualNoAlvo}%` : '—' }}
          </strong>
        </div>
      </div>

      <!-- RN20 — a faixa em uso fica explícita -->
      <p class="faixa numerico">
        Faixa alvo {{ historico.resumo.minAlvoAplicado }}–{{ historico.resumo.maxAlvoAplicado }} mg/dL
        <span v-if="!historico.resumo.faixaPersonalizada" class="selo selo-alerta">padrão, não personalizada</span>
      </p>

      <GraficoLinha
        :pontos="pontos"
        :min-alvo="serie?.minAlvo"
        :max-alvo="serie?.maxAlvo"
        unidade="mg/dL"
      />

      <table v-if="historico.registros.length" class="tabela lista">
        <thead>
          <tr><th>Data e hora</th><th>Valor</th><th>Contexto</th><th>Situação</th><th>Observação</th><th></th></tr>
        </thead>
        <tbody>
          <tr v-for="r in historico.registros" :key="r.id">
            <td class="numerico">{{ quando(r.dataHora) }}</td>
            <td class="numerico" :class="{ fora: r.foraDoAlvo }"><strong>{{ r.valor }}</strong> mg/dL</td>
            <td>{{ r.contexto }}</td>
            <td>
              <span class="selo" :class="seloClassificacao(r.classificacao)">
                {{ textoClassificacao(r.classificacao) }}
              </span>
              <span v-if="r.foraDoAlvo && r.classificacao === 'NORMAL'" class="selo selo-alerta">
                fora do alvo
              </span>
            </td>
            <td class="obs">{{ r.observacao || '—' }}</td>
            <td class="acao"><button class="remover" @click="remover(r.id)">remover</button></td>
          </tr>
        </tbody>
      </table>
    </template>
  </section>
</template>

<style scoped>
.topo { margin-bottom: var(--md); }
.card h3 { font-size: 15px; color: var(--text-secondary); }
.btn.pequeno { padding: 6px 14px; font-size: 14px; }

.periodos { display: flex; border: 1px solid var(--border); border-radius: var(--raio); overflow: hidden; }
.periodo {
  font-family: inherit; font-size: 13px; padding: 6px 12px;
  background: var(--surface); border: none; cursor: pointer; color: var(--text-secondary);
}
.periodo + .periodo { border-left: 1px solid var(--border); }
.periodo.ativo { background: var(--teal-light); color: var(--primary); font-weight: 500; }

.formulario { padding: var(--md); background: var(--background); border: 1px solid var(--border); border-radius: var(--raio); margin-bottom: var(--md); }
.acoes { display: flex; gap: var(--xs); }

.indicadores { display: flex; gap: var(--lg); flex-wrap: wrap; margin-bottom: var(--xs); }
.indicadores > div { display: flex; flex-direction: column; }
.rotulo { font-size: 11px; letter-spacing: 0.05em; text-transform: uppercase; color: var(--text-muted); }
.indicadores strong { font-size: 20px; }
.bom { color: var(--primary); }
.ruim { color: var(--danger); }

.faixa { font-size: 13px; color: var(--text-secondary); margin: 0 0 var(--md); display: flex; align-items: center; gap: var(--xs); }

.lista { margin-top: var(--md); }
.lista .fora strong { color: var(--danger); }
.lista .selo + .selo { margin-left: 4px; }
.obs { color: var(--text-secondary); font-size: 13px; }
.acao { text-align: right; }
.remover {
  background: none; border: none; color: var(--danger);
  font-family: inherit; font-size: 12px; cursor: pointer; padding: 0;
}
</style>
