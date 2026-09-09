<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { mensagemDeErro } from '../api/client'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

const senhaAtual = ref('')
const novaSenha = ref('')
const confirmacao = ref('')
const erro = ref('')
const enviando = ref(false)

const naoConfere = computed(
  () => confirmacao.value.length > 0 && novaSenha.value !== confirmacao.value,
)
const curta = computed(() => novaSenha.value.length > 0 && novaSenha.value.length < 8)
const podeEnviar = computed(
  () => !!senhaAtual.value && novaSenha.value.length >= 8 && !naoConfere.value,
)

async function trocar() {
  erro.value = ''
  enviando.value = true
  try {
    await auth.alterarSenha(senhaAtual.value, novaSenha.value)
    router.push({ name: auth.ehNutricionista ? 'dashboard' : 'minha-area' })
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível alterar a senha.')
  } finally {
    enviando.value = false
  }
}

function sair() {
  auth.sair()
  router.push({ name: 'login' })
}
</script>

<template>
  <div class="tela">
    <form class="card caixa" @submit.prevent="trocar">
      <h2>Defina sua senha</h2>
      <!-- RN03 — a senha provisória não serve para mais nada além desta troca. -->
      <p class="subtitulo">
        Você entrou com a senha provisória enviada por e-mail. Escolha uma senha
        definitiva para liberar o acesso ao sistema.
      </p>

      <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>

      <div class="campo">
        <label for="atual">Senha provisória</label>
        <input id="atual" v-model="senhaAtual" type="password" autocomplete="current-password" required />
      </div>

      <div class="campo">
        <label for="nova">Nova senha</label>
        <input id="nova" v-model="novaSenha" type="password" autocomplete="new-password" required />
        <span v-if="curta" class="erro-campo">A senha deve ter ao menos 8 caracteres.</span>
        <span v-else class="ajuda">Mínimo de 8 caracteres.</span>
      </div>

      <div class="campo">
        <label for="confirma">Confirme a nova senha</label>
        <input id="confirma" v-model="confirmacao" type="password" autocomplete="new-password" required />
        <span v-if="naoConfere" class="erro-campo">As senhas não coincidem.</span>
      </div>

      <button class="btn btn-primario largo" type="submit" :disabled="enviando || !podeEnviar">
        {{ enviando ? 'Salvando…' : 'Salvar e entrar' }}
      </button>
      <button class="btn btn-secundario largo" type="button" @click="sair">Sair</button>
    </form>
  </div>
</template>

<style scoped>
.tela { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: var(--md); }
.caixa { width: 100%; max-width: 420px; }
.subtitulo { margin: var(--xs) 0 var(--lg); font-size: 14px; color: var(--text-secondary); }
.largo { width: 100%; margin-top: var(--xs); }
</style>
