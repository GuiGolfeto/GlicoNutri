<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute } from 'vue-router'
import { api, mensagemDeErro } from '../api/client'
import type { Paciente } from '../api/tipos'
import PainelAntropometria from '../components/PainelAntropometria.vue'
import PainelEnergetica from '../components/PainelEnergetica.vue'

const rota = useRoute()
const paciente = ref<Paciente | null>(null)
const carregando = ref(true)
const erro = ref('')
const sucesso = ref('')

const metas = reactive({ glicemiaMinAlvo: null as number | null, glicemiaMaxAlvo: null as number | null })
const salvandoMetas = ref(false)

/** Sobe a cada medição nova, para o painel do VET saber que a base mudou. */
const versaoAntropometria = ref(0)

async function carregar() {
  carregando.value = true
  try {
    const { data } = await api.get<Paciente>(`/pacientes/${rota.params.id}`)
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

/** RN20 — faixa glicêmica definida individualmente pelo nutricionista. */
async function salvarMetas() {
  erro.value = ''
  sucesso.value = ''
  salvandoMetas.value = true
  try {
    const { data } = await api.put<Paciente>(`/pacientes/${rota.params.id}/metas-glicemicas`, metas)
    paciente.value = data
    sucesso.value = 'Faixa glicêmica alvo atualizada.'
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    salvandoMetas.value = false
  }
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
        </p>
      </div>
      <span class="selo" :class="paciente.ativo ? 'selo-ok' : 'selo-neutro'">
        {{ paciente.ativo ? 'Ativo' : 'Inativo' }}
      </span>
    </div>

    <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>
    <div v-if="sucesso" class="aviso aviso-sucesso">{{ sucesso }}</div>

    <div class="colunas">
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

        <!-- RN20 — sem personalização, o sistema usa o padrão clínico e avisa. -->
        <div v-if="!paciente.faixaGlicemicaPersonalizada" class="aviso aviso-atencao">
          Ainda não personalizada. O sistema está usando o padrão clínico de
          <strong class="numerico">70–180 mg/dL</strong> para classificar os registros deste paciente.
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
        </form>
      </section>
    </div>

    <div class="colunas secao">
      <PainelAntropometria
        :paciente-id="paciente.id"
        @registrado="versaoAntropometria++"
      />
      <PainelEnergetica
        :paciente-id="paciente.id"
        :recarregar="versaoAntropometria"
      />
    </div>

    <p class="proximos">
      Registros glicêmicos e plano alimentar entram nas próximas etapas.
    </p>
  </template>
</template>

<style scoped>
.voltar { font-size: 14px; }
.cabecalho { margin: var(--xs) 0 var(--lg); align-items: flex-start; }
.sub { margin: 4px 0 0; color: var(--text-secondary); font-size: 15px; }

.colunas { display: grid; grid-template-columns: 1fr 1fr; gap: var(--md); align-items: start; }
@media (max-width: 900px) { .colunas { grid-template-columns: 1fr; } }

.card h3 { font-size: 15px; color: var(--text-secondary); margin-bottom: var(--md); }

dl { display: grid; grid-template-columns: 128px 1fr; gap: var(--xs) var(--md); margin: 0; font-size: 15px; }
dt { color: var(--text-muted); font-size: 14px; }
dd { margin: 0; }

.secao { margin-top: var(--md); }
.proximos { margin-top: var(--lg); font-size: 14px; color: var(--text-muted); }
</style>
