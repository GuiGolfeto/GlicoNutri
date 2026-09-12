<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type { CorrelacaoEmocional, DiarioEmocional } from '../api/tipos'

const props = defineProps<{ pacienteId: number }>()

const diario = ref<DiarioEmocional | null>(null)
const correlacao = ref<CorrelacaoEmocional | null>(null)
const carregando = ref(true)
const erro = ref('')

async function carregar() {
  carregando.value = true
  try {
    const [d, c] = await Promise.all([
      api.get<DiarioEmocional>(`/pacientes/${props.pacienteId}/emocoes`, { params: { dias: 30 } }),
      api.get<CorrelacaoEmocional>(`/pacientes/${props.pacienteId}/emocoes/correlacao`, { params: { dias: 30 } }),
    ])
    diario.value = d.data
    correlacao.value = c.data
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar o diário emocional.')
  } finally {
    carregando.value = false
  }
}

onMounted(carregar)

async function remover(id: number) {
  erro.value = ''
  try {
    await api.delete(`/pacientes/${props.pacienteId}/emocoes/${id}`)
    await carregar()
  } catch (e) {
    erro.value = mensagemDeErro(e)
  }
}

const intensidade = (n: number) => '●'.repeat(n) + '○'.repeat(5 - n)

const quando = (iso: string) =>
  new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })

/** Destaca a emoção cuja glicemia média mais se afasta da média geral. */
const desvio = (media: number | null) => {
  const geral = correlacao.value?.mediaGlicemiaGeral
  if (media == null || geral == null) return null
  return Math.round(media - geral)
}
</script>

<template>
  <section class="card">
    <h3>Diário emocional</h3>

    <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>
    <p v-if="carregando" class="vazio">Carregando…</p>

    <template v-else>
      <p v-if="!diario?.totalRegistros" class="vazio">
        O paciente ainda não registrou emoções pelo aplicativo.
      </p>

      <template v-else>
        <!-- RF06.2 — correlação entre emoção e variação glicêmica -->
        <template v-if="correlacao && correlacao.porEmocao.length">
          <p class="apoio">
            Glicemias medidas até {{ correlacao.janelaHoras }}h de cada registro emocional.
            Média geral do período: <strong class="numerico">{{ correlacao.mediaGlicemiaGeral ?? '—' }}</strong> mg/dL.
          </p>

          <table class="tabela">
            <thead>
              <tr>
                <th>Emoção</th><th>Registros</th><th>Glicemias</th>
                <th>Média</th><th>vs. geral</th><th>Fora do alvo</th><th>Intensidade</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="c in correlacao.porEmocao" :key="c.estadoEmocional">
                <td>{{ c.estadoEmocional }}</td>
                <td class="numerico">{{ c.registrosEmocionais }}</td>
                <td class="numerico">{{ c.glicemiasAssociadas }}</td>
                <td class="numerico">{{ c.mediaGlicemia ?? '—' }}</td>
                <td class="numerico">
                  <span v-if="desvio(c.mediaGlicemia) != null"
                        :class="desvio(c.mediaGlicemia)! > 0 ? 'alta' : 'baixa'">
                    {{ desvio(c.mediaGlicemia)! > 0 ? '+' : '' }}{{ desvio(c.mediaGlicemia) }}
                  </span>
                  <span v-else>—</span>
                </td>
                <td class="numerico">
                  {{ c.percentualForaDoAlvo != null ? `${c.percentualForaDoAlvo}%` : '—' }}
                </td>
                <td class="escala">{{ intensidade(Math.round(c.intensidadeMedia)) }}</td>
              </tr>
            </tbody>
          </table>
        </template>

        <details class="registros">
          <summary>{{ diario.totalRegistros }} registro(s) nos últimos {{ diario.dias }} dias</summary>
          <ul>
            <li v-for="r in diario.registros" :key="r.id">
              <span class="numerico data">{{ quando(r.dataHora) }}</span>
              <strong>{{ r.estadoEmocional }}</strong>
              <span class="escala">{{ intensidade(r.intensidade) }}</span>
              <span v-if="r.descricao" class="descricao">{{ r.descricao }}</span>
              <button class="remover" @click="remover(r.id)">Remover</button>
            </li>
          </ul>
        </details>
      </template>
    </template>
  </section>
</template>

<style scoped>
.card h3 { font-size: 15px; color: var(--text-secondary); margin-bottom: var(--md); }
.apoio { font-size: 13px; color: var(--text-secondary); margin: 0 0 var(--md); }

.escala { color: var(--primary); letter-spacing: 1px; font-size: 12px; }
.alta { color: var(--warning); }
.baixa { color: var(--success); }

.registros { margin-top: var(--md); }
.registros summary { font-size: 13px; color: var(--text-muted); cursor: pointer; }
.registros ul { list-style: none; margin: var(--sm) 0 0; padding: 0; }
.registros li {
  display: flex; align-items: center; gap: var(--xs);
  padding: 6px 0; border-bottom: 1px solid var(--border); font-size: 14px;
}
.data { color: var(--text-muted); font-size: 12px; min-width: 100px; }
.descricao { color: var(--text-secondary); font-size: 13px; flex: 1; }
.remover {
  background: none; border: none; color: var(--danger);
  font-family: inherit; font-size: 12px; cursor: pointer; padding: 0;
}
</style>
