<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type { Nutricionista } from '../api/tipos'

const nutricionistas = ref<Nutricionista[]>([])
const incluirInativos = ref(false)
const carregando = ref(true)
const erro = ref('')

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const { data } = await api.get<Nutricionista[]>('/nutricionistas', {
      params: { incluirInativos: incluirInativos.value },
    })
    nutricionistas.value = data
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar os nutricionistas.')
  } finally {
    carregando.value = false
  }
}

onMounted(carregar)

async function alternarSituacao(n: Nutricionista) {
  erro.value = ''
  try {
    if (n.ativo) await api.delete(`/nutricionistas/${n.id}`)
    else await api.post(`/nutricionistas/${n.id}/reativar`)
    await carregar()
  } catch (e) {
    // A API recusa desativar quem ainda tem paciente vinculado (RN07).
    erro.value = mensagemDeErro(e)
  }
}
</script>

<template>
  <div class="entre cabecalho">
    <div>
      <h1>Nutricionistas</h1>
      <p class="sub">Profissionais com acesso ao sistema web</p>
    </div>
    <RouterLink class="btn btn-primario" :to="{ name: 'nutricionista-novo' }">Novo Nutricionista</RouterLink>
  </div>

  <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>

  <div class="card sem-padding">
    <div class="barra">
      <label class="linha filtro">
        <input v-model="incluirInativos" type="checkbox" @change="carregar" />
        Mostrar inativos
      </label>
      <span class="contador numerico">{{ nutricionistas.length }}</span>
    </div>

    <p v-if="carregando" class="vazio">Carregando…</p>
    <p v-else-if="nutricionistas.length === 0" class="vazio">Nenhum nutricionista cadastrado.</p>

    <table v-else class="tabela">
      <thead>
        <tr>
          <th>Nome</th><th>CRN</th><th>Especialidade</th>
          <th>Pacientes</th><th>Situação</th><th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="n in nutricionistas" :key="n.id">
          <td>
            {{ n.nome }}
            <span class="email">{{ n.email }}</span>
          </td>
          <td class="numerico">{{ n.crn }}</td>
          <td>{{ n.especialidade || '—' }}</td>
          <td class="numerico">{{ n.totalPacientes }}</td>
          <td>
            <span v-if="!n.ativo" class="selo selo-neutro">Inativo</span>
            <!-- RN03 — ainda não fez o primeiro acesso. -->
            <span v-else-if="n.senhaProvisoria" class="selo selo-alerta">Aguardando 1º acesso</span>
            <span v-else class="selo selo-ok">Ativo</span>
          </td>
          <td class="acao">
            <RouterLink class="btn btn-secundario" :to="{ name: 'nutricionista-editar', params: { id: n.id } }">
              Editar
            </RouterLink>
            <button class="btn" :class="n.ativo ? 'btn-perigo' : 'btn-secundario'" @click="alternarSituacao(n)">
              {{ n.ativo ? 'Desativar' : 'Reativar' }}
            </button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.cabecalho { margin-bottom: var(--lg); }
.sub { margin: 4px 0 0; color: var(--text-secondary); font-size: 15px; }
.sem-padding { padding: 0; }
.barra { display: flex; align-items: center; justify-content: space-between; padding: var(--md) var(--md) var(--sm); }
.filtro { font-size: 14px; color: var(--text-secondary); cursor: pointer; }
.contador { font-size: 13px; color: var(--text-muted); }
.email { display: block; font-size: 13px; color: var(--text-muted); }
.acao { text-align: right; }
.acao .btn { padding: 5px 12px; font-size: 13px; margin-left: 4px; }
</style>
