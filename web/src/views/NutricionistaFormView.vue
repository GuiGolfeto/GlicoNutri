<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { api, mensagemDeErro } from '../api/client'
import type { CriarNutricionista } from '../api/tipos'

const router = useRouter()
const erro = ref('')
const enviando = ref(false)

const form = reactive<CriarNutricionista>({
  nome: '', email: '', crn: '', especialidade: '', telefone: '',
})

const podeEnviar = computed(() => !!form.nome && !!form.email && !!form.crn)

async function salvar() {
  erro.value = ''
  enviando.value = true
  try {
    await api.post('/nutricionistas', form)
    router.push({ name: 'nutricionistas' })
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <h1>Novo Nutricionista</h1>
  <p class="sub">
    O sistema gera uma senha provisória e a envia por e-mail. O profissional
    precisa defini-la no primeiro acesso antes de usar o sistema.
  </p>

  <form class="card formulario" @submit.prevent="salvar">
    <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>

    <div class="campo">
      <label for="nome">Nome completo *</label>
      <input id="nome" v-model="form.nome" required />
    </div>

    <div class="grade-2">
      <div class="campo">
        <label for="crn">CRN *</label>
        <input id="crn" v-model="form.crn" placeholder="CRN3-00000" required />
        <span class="ajuda">Único no sistema.</span>
      </div>
      <div class="campo">
        <label for="especialidade">Especialidade</label>
        <input id="especialidade" v-model="form.especialidade" />
      </div>
    </div>

    <div class="grade-2">
      <div class="campo">
        <label for="email">E-mail profissional *</label>
        <input id="email" v-model="form.email" type="email" required />
      </div>
      <div class="campo">
        <label for="telefone">Telefone</label>
        <input id="telefone" v-model="form.telefone" />
      </div>
    </div>

    <div class="acoes">
      <button class="btn btn-primario" type="submit" :disabled="enviando || !podeEnviar">
        {{ enviando ? 'Salvando…' : 'Cadastrar nutricionista' }}
      </button>
      <RouterLink class="btn btn-secundario" :to="{ name: 'nutricionistas' }">Cancelar</RouterLink>
    </div>
  </form>
</template>

<style scoped>
.sub { margin: 4px 0 var(--lg); color: var(--text-secondary); font-size: 15px; max-width: 620px; }
.formulario { max-width: 720px; }
.acoes { display: flex; gap: var(--sm); margin-top: var(--lg); padding-top: var(--lg); border-top: 1px solid var(--border); }
</style>
