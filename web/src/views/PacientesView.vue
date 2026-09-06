<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type { Paciente } from '../api/tipos'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const pacientes = ref<Paciente[]>([])
const busca = ref('')
const carregando = ref(true)
const erro = ref('')

async function carregar() {
  carregando.value = true
  try {
    const { data } = await api.get<Paciente[]>('/pacientes')
    pacientes.value = data
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar os pacientes.')
  } finally {
    carregando.value = false
  }
}

onMounted(carregar)

function filtrados() {
  const termo = busca.value.trim().toLowerCase()
  if (!termo) return pacientes.value
  return pacientes.value.filter(
    (p) => p.nome.toLowerCase().includes(termo) || p.cpf.includes(termo.replace(/\D/g, '')),
  )
}

const mascararCpf = (cpf: string) =>
  cpf.replace(/^(\d{3})(\d{3})(\d{3})(\d{2})$/, '$1.$2.$3-$4')
</script>

<template>
  <div class="entre cabecalho">
    <div>
      <h1>Pacientes</h1>
      <p class="sub">
        {{ auth.ehAdministrador ? 'Todos os pacientes da associação' : 'Pacientes sob sua responsabilidade' }}
      </p>
    </div>
    <RouterLink class="btn btn-primario" :to="{ name: 'paciente-novo' }">Novo Paciente</RouterLink>
  </div>

  <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>

  <div class="card sem-padding">
    <div class="barra">
      <input v-model="busca" class="busca" type="search" placeholder="Buscar por nome ou CPF…" />
      <span class="contador numerico">{{ filtrados().length }} de {{ pacientes.length }}</span>
    </div>

    <p v-if="carregando" class="vazio">Carregando…</p>

    <p v-else-if="pacientes.length === 0" class="vazio">
      Nenhum paciente cadastrado ainda.<br />
      <RouterLink :to="{ name: 'paciente-novo' }">Cadastrar o primeiro paciente</RouterLink>
    </p>

    <p v-else-if="filtrados().length === 0" class="vazio">Nenhum paciente corresponde à busca.</p>

    <table v-else class="tabela">
      <thead>
        <tr>
          <th>Nome</th>
          <th>CPF</th>
          <th>Idade</th>
          <th>Tipo</th>
          <th>Faixa alvo</th>
          <th v-if="auth.ehAdministrador">Responsável</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="p in filtrados()" :key="p.id">
          <td>
            <RouterLink :to="{ name: 'paciente-detalhe', params: { id: p.id } }">{{ p.nome }}</RouterLink>
          </td>
          <td class="numerico">{{ mascararCpf(p.cpf) }}</td>
          <td class="numerico">{{ p.idade }}</td>
          <td>{{ p.tipoDiabetes }}</td>
          <td>
            <!-- RN20 — sinaliza quando a faixa ainda é o padrão clínico. -->
            <span v-if="p.faixaGlicemicaPersonalizada" class="selo selo-ok numerico">
              {{ p.glicemiaMinAlvo }}–{{ p.glicemiaMaxAlvo }} mg/dL
            </span>
            <span v-else class="selo selo-alerta">Padrão 70–180</span>
          </td>
          <td v-if="auth.ehAdministrador">{{ p.nutricionistaNome ?? '—' }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.cabecalho { margin-bottom: var(--lg); }
.sub { margin: 4px 0 0; color: var(--text-secondary); font-size: 15px; }
.sem-padding { padding: 0; }
.barra { display: flex; align-items: center; gap: var(--md); padding: var(--md) var(--md) var(--sm); }
.busca {
  flex: 1;
  font-family: inherit; font-size: 15px;
  padding: 9px 12px;
  border: 1px solid var(--border);
  border-radius: var(--raio);
  background: var(--background);
}
.busca:focus { outline: none; border-color: var(--primary); box-shadow: 0 0 0 3px rgba(0, 104, 93, 0.12); }
.contador { font-size: 13px; color: var(--text-muted); white-space: nowrap; }
</style>
