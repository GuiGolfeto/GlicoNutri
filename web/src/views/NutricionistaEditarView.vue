<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api, mensagemDeErro } from '../api/client'
import type { Nutricionista } from '../api/tipos'

const rota = useRoute()
const router = useRouter()
const id = Number(rota.params.id)

const carregando = ref(true)
const enviando = ref(false)
const erro = ref('')

const form = reactive({ nome: '', email: '', crn: '', especialidade: '', telefone: '' })

onMounted(async () => {
  try {
    const { data } = await api.get<Nutricionista>(`/nutricionistas/${id}`)
    Object.assign(form, {
      nome: data.nome, email: data.email, crn: data.crn,
      especialidade: data.especialidade ?? '', telefone: data.telefone ?? '',
    })
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar o nutricionista.')
  } finally {
    carregando.value = false
  }
})

async function salvar() {
  erro.value = ''
  enviando.value = true
  try {
    await api.put(`/nutricionistas/${id}`, form)
    router.push({ name: 'nutricionistas' })
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <h1>Editar Nutricionista</h1>
  <p class="sub">Alterar o e-mail muda também o login do profissional.</p>

  <p v-if="carregando" class="vazio">Carregando…</p>

  <form v-else class="card formulario" @submit.prevent="salvar">
    <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>

    <div class="campo">
      <label for="nome">Nome completo *</label>
      <input id="nome" v-model="form.nome" required />
    </div>

    <div class="grade-2">
      <div class="campo">
        <label for="crn">CRN *</label>
        <input id="crn" v-model="form.crn" required />
      </div>
      <div class="campo">
        <label for="especialidade">Especialidade</label>
        <input id="especialidade" v-model="form.especialidade" />
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

    <div class="acoes">
      <button class="btn btn-primario" type="submit" :disabled="enviando">
        {{ enviando ? 'Salvando…' : 'Salvar alterações' }}
      </button>
      <RouterLink class="btn btn-secundario" :to="{ name: 'nutricionistas' }">Cancelar</RouterLink>
    </div>
  </form>
</template>

<style scoped>
.sub { margin: 4px 0 var(--lg); color: var(--text-secondary); font-size: 15px; }
.formulario { max-width: 720px; }
.acoes { display: flex; gap: var(--sm); margin-top: var(--lg); padding-top: var(--lg); border-top: 1px solid var(--border); }
</style>
