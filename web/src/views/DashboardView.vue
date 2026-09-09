<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type { Dashboard, Severidade } from '../api/tipos'
import BarrasDistribuicao from '../components/BarrasDistribuicao.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const dados = ref<Dashboard | null>(null)
const carregando = ref(true)
const erro = ref('')

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const { data } = await api.get<Dashboard>('/dashboard')
    dados.value = data
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar o painel.')
  } finally {
    carregando.value = false
  }
}

onMounted(carregar)

const selo = (s: Severidade | null) =>
  s === 'CRITICO' ? 'selo-perigo' : s === 'ATENCAO' ? 'selo-alerta' : s ? 'selo-neutro' : 'selo-ok'

const rotulo = (s: Severidade | null) =>
  s === 'CRITICO' ? 'Crítico' : s === 'ATENCAO' ? 'Atenção' : s ? 'Informativo' : 'Em dia'

/** Apenas as pendências que pedem ação; as informativas ficam no rodapé. */
const acionaveis = computed(() =>
  dados.value?.alertas.filter((a) => a.severidade !== 'INFORMATIVO') ?? [])

const informativas = computed(() =>
  dados.value?.alertas.filter((a) => a.severidade === 'INFORMATIVO') ?? [])

/**
 * Cor por significado da classificação, não por posição na lista. A ordenação é
 * por quantidade, então índice não diz nada: "Sem medição" saía em verde e
 * "Peso normal" em laranja quando a contagem invertia.
 */
const CORES_IMC: Record<string, string> = {
  'Peso normal': 'var(--success)',
  'Sobrepeso': 'var(--warning)',
  'Obesidade': 'var(--danger)',
  'Abaixo do peso': 'var(--info)',
  'Sem medição': 'var(--text-muted)',
}

const coresImc = computed(() =>
  (dados.value?.classificacaoImc ?? []).map((f) => CORES_IMC[f.rotulo] ?? 'var(--text-muted)'))

const hora = (iso: string | null) =>
  iso ? new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' }) : '—'
</script>

<template>
  <div class="entre cabecalho">
    <div>
      <h1>Dashboard</h1>
      <p class="sub">
        {{ auth.ehAdministrador ? 'Todos os pacientes da associação' : 'Seus pacientes em acompanhamento' }}
      </p>
    </div>
    <button class="btn btn-secundario" :disabled="carregando" @click="carregar">
      {{ carregando ? 'Atualizando…' : 'Atualizar' }}
    </button>
  </div>

  <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>
  <p v-if="carregando && !dados" class="vazio">Carregando…</p>

  <template v-if="dados">
    <!-- Cartões -->
    <div class="cartoes">
      <div class="card cartao">
        <span class="rotulo">Total de pacientes</span>
        <strong class="numero numerico">{{ dados.indicadores.totalPacientes }}</strong>
      </div>
      <div class="card cartao">
        <span class="rotulo">Com registro hoje</span>
        <strong class="numero numerico">{{ dados.indicadores.pacientesComRegistroHoje }}</strong>
      </div>
      <div class="card cartao" :class="{ atencao: dados.indicadores.pacientesComAlerta > 0 }">
        <span class="rotulo">Com pendência</span>
        <strong class="numero numerico">{{ dados.indicadores.pacientesComAlerta }}</strong>
      </div>
      <div class="card cartao">
        <span class="rotulo">Planos ativos</span>
        <strong class="numero numerico">{{ dados.indicadores.planosAtivos }}</strong>
      </div>
    </div>

    <div class="colunas">
      <!-- Pendências, ordenadas por criticidade (RN03 do UC011) -->
      <section class="card">
        <h3>Pendências</h3>

        <p v-if="acionaveis.length === 0" class="tudo-certo">
          Nenhuma pendência que exija ação agora.
        </p>

        <ul v-else class="pendencias">
          <li v-for="(a, i) in acionaveis" :key="i">
            <span class="selo" :class="selo(a.severidade)">{{ rotulo(a.severidade) }}</span>
            <div class="texto">
              <RouterLink :to="{ name: 'paciente-detalhe', params: { id: a.pacienteId } }">
                {{ a.pacienteNome }}
              </RouterLink>
              <span class="descricao">{{ a.descricao }}</span>
            </div>
            <span class="quando numerico">{{ hora(a.ocorrencia) }}</span>
          </li>
        </ul>

        <details v-if="informativas.length" class="informativas">
          <summary>{{ informativas.length }} aviso(s) informativo(s)</summary>
          <ul class="pendencias">
            <li v-for="(a, i) in informativas" :key="i">
              <span class="selo selo-neutro">Info</span>
              <div class="texto">
                <RouterLink :to="{ name: 'paciente-detalhe', params: { id: a.pacienteId } }">
                  {{ a.pacienteNome }}
                </RouterLink>
                <span class="descricao">{{ a.descricao }}</span>
              </div>
            </li>
          </ul>
        </details>
      </section>

      <div class="lateral">
        <section class="card">
          <h3>Controle glicêmico</h3>
          <p class="apoio numerico">
            Média geral {{ dados.indicadores.mediaGlicemicaGeral ?? '—' }} mg/dL ·
            {{ dados.indicadores.mediaPercentualNoAlvo ?? '—' }}% no alvo
          </p>
          <BarrasDistribuicao :fatias="dados.controleGlicemico" />
        </section>

        <section class="card">
          <h3>Classificação de IMC</h3>
          <BarrasDistribuicao :fatias="dados.classificacaoImc" :cores="coresImc" />
        </section>
      </div>
    </div>

    <!-- Lista de pacientes -->
    <section class="card sem-padding tabela-pacientes">
      <div class="titulo-tabela">
        <h3>Pacientes</h3>
        <RouterLink :to="{ name: 'pacientes' }" class="ver-todos">Ver lista completa →</RouterLink>
      </div>

      <p v-if="dados.pacientes.length === 0" class="vazio">
        Nenhum paciente cadastrado ainda.
      </p>

      <table v-else class="tabela">
        <thead>
          <tr>
            <th>Situação</th><th>Paciente</th><th>Última glicemia</th>
            <th>Média 7d</th><th>No alvo</th><th>IMC</th><th>Plano</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in dados.pacientes" :key="p.pacienteId">
            <td><span class="selo" :class="selo(p.severidade)">{{ rotulo(p.severidade) }}</span></td>
            <td>
              <RouterLink :to="{ name: 'paciente-detalhe', params: { id: p.pacienteId } }">
                {{ p.nome }}
              </RouterLink>
              <span v-if="p.diasSemRegistroGlicemia != null && p.diasSemRegistroGlicemia >= 3" class="nota">
                sem registro há {{ p.diasSemRegistroGlicemia }} dias
              </span>
            </td>
            <td class="numerico">
              <template v-if="p.ultimaGlicemia != null">
                {{ p.ultimaGlicemia }} mg/dL
                <span class="nota">{{ p.ultimaGlicemiaContexto }} · {{ hora(p.ultimaGlicemiaData) }}</span>
              </template>
              <span v-else class="nota">—</span>
            </td>
            <td class="numerico">{{ p.mediaGlicemia7Dias ?? '—' }}</td>
            <td class="numerico">{{ p.percentualNoAlvo7Dias != null ? `${p.percentualNoAlvo7Dias}%` : '—' }}</td>
            <td class="numerico">
              {{ p.ultimoImc ?? '—' }}
              <span v-if="p.classificacaoImc" class="nota">{{ p.classificacaoImc }}</span>
            </td>
            <td>
              <span class="selo" :class="p.planoAtivo ? 'selo-ok' : 'selo-alerta'">
                {{ p.planoAtivo ? 'Ativo' : 'Sem plano' }}
              </span>
            </td>
          </tr>
        </tbody>
      </table>
    </section>
  </template>
</template>

<style scoped>
.cabecalho { margin-bottom: var(--lg); }
.sub { margin: 4px 0 0; color: var(--text-secondary); font-size: 15px; }

.cartoes { display: grid; grid-template-columns: repeat(4, 1fr); gap: var(--md); margin-bottom: var(--md); }
@media (max-width: 900px) { .cartoes { grid-template-columns: repeat(2, 1fr); } }

.cartao { padding: var(--md); }
.cartao.atencao { border-color: #fde68a; background: #fffbeb; }
.rotulo { font-size: 12px; letter-spacing: 0.05em; text-transform: uppercase; color: var(--text-muted); }
.numero { display: block; font-size: 30px; line-height: 1.1; margin-top: 4px; }

.colunas { display: grid; grid-template-columns: 1.6fr 1fr; gap: var(--md); align-items: start; margin-bottom: var(--md); }
@media (max-width: 900px) { .colunas { grid-template-columns: 1fr; } }
.lateral { display: flex; flex-direction: column; gap: var(--md); }

.card h3 { font-size: 15px; color: var(--text-secondary); margin-bottom: var(--md); }
.apoio { font-size: 13px; color: var(--text-secondary); margin: -8px 0 var(--sm); }

.pendencias { list-style: none; margin: 0; padding: 0; }
.pendencias li {
  display: flex; align-items: flex-start; gap: var(--xs);
  padding: var(--xs) 0; border-bottom: 1px solid var(--border);
}
.pendencias li:last-child { border-bottom: none; }
.pendencias .texto { flex: 1; display: flex; flex-direction: column; }
.descricao { font-size: 13px; color: var(--text-secondary); }
.quando { font-size: 12px; color: var(--text-muted); white-space: nowrap; }

.tudo-certo { color: var(--success); font-size: 14px; margin: 0; }
.informativas { margin-top: var(--md); }
.informativas summary { font-size: 13px; color: var(--text-muted); cursor: pointer; }

.sem-padding { padding: 0; }
.titulo-tabela { display: flex; align-items: center; justify-content: space-between; padding: var(--md) var(--md) var(--sm); }
.titulo-tabela h3 { margin: 0; }
.ver-todos { font-size: 14px; }
.nota { display: block; font-size: 12px; color: var(--text-muted); }
</style>
