<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api, mensagemDeErro } from '../api/client'
import type { Paciente, Referencia } from '../api/tipos'

const rota = useRoute()
const router = useRouter()
const id = Number(rota.params.id)

const sexos = ref<Referencia[]>([])
const tipos = ref<Referencia[]>([])
const carregando = ref(true)
const enviando = ref(false)
const erro = ref('')

const form = reactive({
  nome: '', email: '', cpf: '', dataNascimento: '',
  sexoId: null as number | null, tipoDiabetesId: null as number | null,
  telefone: '', medicacaoEmUso: '', observacoesClinicas: '',
})

onMounted(async () => {
  try {
    const [p, s, t] = await Promise.all([
      api.get<Paciente>(`/pacientes/${id}`),
      api.get<Referencia[]>('/referencias/sexos-biologicos'),
      api.get<Referencia[]>('/referencias/tipos-diabetes'),
    ])
    sexos.value = s.data
    tipos.value = t.data

    // O detalhe devolve as descrições; o formulário precisa dos ids.
    Object.assign(form, {
      nome: p.data.nome,
      email: p.data.email,
      cpf: p.data.cpf,
      dataNascimento: p.data.dataNascimento,
      sexoId: s.data.find((x) => x.descricao === p.data.sexo)?.id ?? null,
      tipoDiabetesId: t.data.find((x) => x.descricao === p.data.tipoDiabetes)?.id ?? null,
      telefone: p.data.telefone ?? '',
      medicacaoEmUso: p.data.medicacaoEmUso ?? '',
      observacoesClinicas: p.data.observacoesClinicas ?? '',
    })
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar o paciente.')
  } finally {
    carregando.value = false
  }
})

async function salvar() {
  erro.value = ''
  enviando.value = true
  try {
    await api.put(`/pacientes/${id}`, form)
    router.push({ name: 'paciente-detalhe', params: { id } })
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <h1>Editar Paciente</h1>
  <p class="sub">Alterar o e-mail muda também o login do paciente no aplicativo.</p>

  <p v-if="carregando" class="vazio">Carregando…</p>

  <form v-else class="card formulario" @submit.prevent="salvar">
    <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>

    <div class="campo">
      <label for="nome">Nome completo *</label>
      <input id="nome" v-model="form.nome" required />
    </div>

    <div class="grade-2">
      <div class="campo">
        <label for="cpf">CPF *</label>
        <input id="cpf" v-model="form.cpf" required />
      </div>
      <div class="campo">
        <label for="nascimento">Data de nascimento *</label>
        <input id="nascimento" v-model="form.dataNascimento" type="date" required />
      </div>
    </div>

    <div class="grade-2">
      <div class="campo">
        <label for="sexo">Sexo *</label>
        <select id="sexo" v-model="form.sexoId" required>
          <option v-for="s in sexos" :key="s.id" :value="s.id">{{ s.descricao }}</option>
        </select>
      </div>
      <div class="campo">
        <label for="tipo">Tipo de diabetes *</label>
        <select id="tipo" v-model="form.tipoDiabetesId" required>
          <option v-for="t in tipos" :key="t.id" :value="t.id">{{ t.descricao }}</option>
        </select>
      </div>
    </div>

    <div class="grade-2">
      <div class="campo">
        <label for="email">E-mail *</label>
        <input id="email" v-model="form.email" type="email" required />
      </div>
      <div class="campo">
        <label for="telefone">Telefone</label>
        <input id="telefone" v-model="form.telefone" />
      </div>
    </div>

    <div class="campo">
      <label for="medicacao">Medicação em uso</label>
      <textarea id="medicacao" v-model="form.medicacaoEmUso" rows="2"></textarea>
    </div>

    <div class="campo">
      <label for="observacoes">Observações clínicas</label>
      <textarea id="observacoes" v-model="form.observacoesClinicas" rows="3"></textarea>
    </div>

    <div class="acoes">
      <button class="btn btn-primario" type="submit" :disabled="enviando">
        {{ enviando ? 'Salvando…' : 'Salvar alterações' }}
      </button>
      <RouterLink class="btn btn-secundario" :to="{ name: 'paciente-detalhe', params: { id } }">
        Cancelar
      </RouterLink>
    </div>
  </form>
</template>

<style scoped>
.sub { margin: 4px 0 var(--lg); color: var(--text-secondary); font-size: 15px; }
/* Centralizado: colado à esquerda, sobrava metade da tela vazia. */
.formulario { max-width: 720px; margin-inline: auto; }
h1, .sub { max-width: 720px; margin-inline: auto; }
.acoes { display: flex; gap: var(--sm); margin-top: var(--lg); padding-top: var(--lg); border-top: 1px solid var(--border); }
</style>
