<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { api, mensagemDeErro } from '../api/client'
import type { Paciente } from '../api/tipos'
import PainelAlertas from '../components/PainelAlertas.vue'
import PainelAntropometria from '../components/PainelAntropometria.vue'
import PainelEmocional from '../components/PainelEmocional.vue'
import PainelEnergetica from '../components/PainelEnergetica.vue'
import PainelGlicemia from '../components/PainelGlicemia.vue'
import PainelPlanoAlimentar from '../components/PainelPlanoAlimentar.vue'

const rota = useRoute()
const paciente = ref<Paciente | null>(null)
const carregando = ref(true)
const erro = ref('')
const sucesso = ref('')

const metas = reactive({ glicemiaMinAlvo: null as number | null, glicemiaMaxAlvo: null as number | null })
const salvandoMetas = ref(false)

/** Sobem a cada escrita, para os painéis dependentes recarregarem. */
const versaoAntropometria = ref(0)
const versaoVet = ref(0)

const abas = [
  { id: 'clinico', rotulo: 'Acompanhamento' },
  { id: 'plano', rotulo: 'Plano alimentar' },
  { id: 'alertas', rotulo: 'Alertas' },
  { id: 'cadastro', rotulo: 'Cadastro' },
] as const

const aba = ref<(typeof abas)[number]['id']>('clinico')
const id = computed(() => Number(rota.params.id))

async function carregar() {
  carregando.value = true
  try {
    const { data } = await api.get<Paciente>(`/pacientes/${id.value}`)
    paciente.value = data
    metas.glicemiaMinAlvo = data.glicemiaMinAlvo
    metas.glicemiaMaxAlvo = data.glicemiaMaxAlvo
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar o paciente.')
  } finally {
    carregando.value = false
  }
}

onMounted(carregar)

/** RN20 — alterar a faixa reclassifica todo o histórico glicêmico. */
async function salvarMetas() {
  erro.value = ''
  sucesso.value = ''
  salvandoMetas.value = true
  try {
    const { data } = await api.put<Paciente>(`/pacientes/${id.value}/metas-glicemicas`, metas)
    paciente.value = data
    sucesso.value = 'Faixa alvo atualizada. Os registros anteriores foram reclassificados.'
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    salvandoMetas.value = false
  }
}

/**
 * RN05 — desativar é soft delete: nenhum dado clínico é apagado, e a reativação
 * restaura o acesso a todo o histórico.
 */
async function alternarSituacao() {
  if (!paciente.value) return
  erro.value = ''
  sucesso.value = ''
  try {
    if (paciente.value.ativo) await api.delete(`/pacientes/${id.value}`)
    else await api.post(`/pacientes/${id.value}/reativar`, {})
    await carregar()
    sucesso.value = paciente.value?.ativo
      ? 'Paciente reativado, com todo o histórico preservado.'
      : 'Paciente desativado. Nenhum dado clínico foi apagado.'
  } catch (e) {
    erro.value = mensagemDeErro(e)
  }
}

function baixarRelatorio(tipo: 'glicemico' | 'antropometrico' | 'completo') {
  const url = `${api.defaults.baseURL}/pacientes/${id.value}/relatorios/${tipo}`
  api.get(url, { responseType: 'blob' }).then((resposta) => {
    const link = document.createElement('a')
    link.href = URL.createObjectURL(resposta.data)
    link.download = `${tipo}-${paciente.value?.nome.split(' ')[0].toLowerCase()}.pdf`
    link.click()
    URL.revokeObjectURL(link.href)
  }).catch((e) => { erro.value = mensagemDeErro(e, 'Não foi possível gerar o relatório.') })
}

const mascararCpf = (cpf: string) => cpf.replace(/^(\d{3})(\d{3})(\d{3})(\d{2})$/, '$1.$2.$3-$4')
const formatarData = (iso: string) => new Date(iso).toLocaleDateString('pt-BR', { timeZone: 'UTC' })
</script>

<template>
  <p v-if="carregando" class="vazio">Carregando…</p>

  <template v-else-if="paciente">
    <div class="entre cabecalho">
      <div>
        <RouterLink :to="{ name: 'pacientes' }" class="voltar">← Pacientes</RouterLink>
        <h1>{{ paciente.nome }}</h1>
        <p class="sub numerico">
          {{ paciente.idade }} anos · {{ paciente.sexo }} · {{ paciente.tipoDiabetes }}
          <template v-if="paciente.nutricionistaNome"> · {{ paciente.nutricionistaNome }}</template>
        </p>
      </div>
      <div class="acoes-topo">
        <span class="selo" :class="paciente.ativo ? 'selo-ok' : 'selo-neutro'">
          {{ paciente.ativo ? 'Ativo' : 'Inativo' }}
        </span>
        <RouterLink class="btn btn-secundario" :to="{ name: 'paciente-editar', params: { id: paciente.id } }">
          Editar
        </RouterLink>
        <button class="btn" :class="paciente.ativo ? 'btn-perigo' : 'btn-secundario'"
                @click="alternarSituacao">
          {{ paciente.ativo ? 'Desativar' : 'Reativar' }}
        </button>
      </div>
    </div>

    <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>
    <div v-if="sucesso" class="aviso aviso-sucesso">{{ sucesso }}</div>

    <nav class="abas">
      <button v-for="a in abas" :key="a.id" class="aba" :class="{ ativa: aba === a.id }"
              @click="aba = a.id">{{ a.rotulo }}</button>
    </nav>

    <!-- Acompanhamento clínico -->
    <template v-if="aba === 'clinico'">
      <PainelGlicemia :paciente-id="paciente.id" />

      <div class="colunas">
        <PainelAntropometria :paciente-id="paciente.id" @registrado="versaoAntropometria++" />
        <PainelEnergetica :paciente-id="paciente.id" :recarregar="versaoAntropometria"
                          @calculado="versaoVet++" />
      </div>

      <PainelEmocional :paciente-id="paciente.id" />

      <section class="card">
        <h3>Relatórios</h3>
        <p class="apoio">Documentos em PDF para o prontuário ou para entregar ao paciente.</p>
        <div class="linha">
          <button class="btn btn-secundario" @click="baixarRelatorio('glicemico')">Glicêmico</button>
          <button class="btn btn-secundario" @click="baixarRelatorio('antropometrico')">Antropométrico</button>
          <button class="btn btn-primario" @click="baixarRelatorio('completo')">Completo</button>
        </div>
      </section>
    </template>

    <!-- Plano alimentar -->
    <PainelPlanoAlimentar v-else-if="aba === 'plano'"
                          :paciente-id="paciente.id" :recarregar="versaoVet" />

    <!-- Alertas -->
    <PainelAlertas v-else-if="aba === 'alertas'" :paciente-id="paciente.id" />

    <!-- Cadastro -->
    <div v-else class="colunas">
      <section class="card">
        <h3>Dados cadastrais</h3>
        <dl>
          <dt>CPF</dt><dd class="numerico">{{ mascararCpf(paciente.cpf) }}</dd>
          <dt>Nascimento</dt><dd class="numerico">{{ formatarData(paciente.dataNascimento) }}</dd>
          <dt>E-mail</dt><dd>{{ paciente.email }}</dd>
          <dt>Telefone</dt><dd>{{ paciente.telefone || '—' }}</dd>
          <dt>Responsável</dt><dd>{{ paciente.nutricionistaNome ?? '—' }}</dd>
          <dt>Medicação</dt><dd>{{ paciente.medicacaoEmUso || '—' }}</dd>
          <dt>Observações</dt><dd>{{ paciente.observacoesClinicas || '—' }}</dd>
        </dl>
      </section>

      <section class="card">
        <h3>Faixa glicêmica alvo</h3>

        <div v-if="!paciente.faixaGlicemicaPersonalizada" class="aviso aviso-atencao">
          Ainda não personalizada. O sistema usa o padrão clínico de
          <strong class="numerico">70–180 mg/dL</strong> para classificar os registros.
        </div>

        <form @submit.prevent="salvarMetas">
          <div class="grade-2">
            <div class="campo">
              <label for="min">Mínimo (mg/dL)</label>
              <input id="min" v-model.number="metas.glicemiaMinAlvo" type="number" min="20" max="600" />
            </div>
            <div class="campo">
              <label for="max">Máximo (mg/dL)</label>
              <input id="max" v-model.number="metas.glicemiaMaxAlvo" type="number" min="20" max="600" />
            </div>
          </div>
          <button class="btn btn-primario" type="submit" :disabled="salvandoMetas">
            {{ salvandoMetas ? 'Salvando…' : 'Salvar faixa alvo' }}
          </button>
          <p class="apoio">
            Alterar a faixa reclassifica também os registros já feitos, para o
            percentual no alvo refletir um critério único.
          </p>
        </form>
      </section>
    </div>
  </template>
</template>

<style scoped>
.voltar { font-size: 14px; }
.cabecalho { margin: var(--xs) 0 var(--md); align-items: flex-start; }
.sub { margin: 4px 0 0; color: var(--text-secondary); font-size: 15px; }
.acoes-topo { display: flex; align-items: center; gap: var(--sm); }

.abas { display: flex; gap: 2px; border-bottom: 1px solid var(--border); margin-bottom: var(--md); }
.aba {
  font-family: inherit; font-size: 15px; padding: 9px 16px;
  background: none; border: none; border-bottom: 2px solid transparent;
  color: var(--text-secondary); cursor: pointer; margin-bottom: -1px;
}
.aba:hover { color: var(--text-primary); }
.aba.ativa { color: var(--primary); border-bottom-color: var(--primary); font-weight: 500; }

.colunas { display: grid; grid-template-columns: 1fr 1fr; gap: var(--md); align-items: start; }
.colunas > * { min-width: 0; }
@media (max-width: 900px) { .colunas { grid-template-columns: 1fr; } }

:deep(.card) { margin-bottom: var(--md); }
.card h3 { font-size: 15px; color: var(--text-secondary); margin-bottom: var(--md); }
.apoio { font-size: 13px; color: var(--text-muted); margin: var(--sm) 0 0; }

dl { display: grid; grid-template-columns: 128px 1fr; gap: var(--xs) var(--md); margin: 0; font-size: 15px; }
dt { color: var(--text-muted); font-size: 14px; }
dd { margin: 0; }
</style>
