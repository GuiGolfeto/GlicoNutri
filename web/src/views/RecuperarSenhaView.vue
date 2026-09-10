<script setup lang="ts">
import { ref } from 'vue'
import { api, mensagemDeErro } from '../api/client'

const email = ref('')
const enviado = ref(false)
const enviando = ref(false)
const erro = ref('')

async function enviar() {
  erro.value = ''
  enviando.value = true
  try {
    await api.post('/auth/recuperar-senha', { email: email.value.trim() })
    // A API responde igual exista ou não a conta; a tela faz o mesmo.
    enviado.value = true
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <div class="tela">
    <div class="card caixa">
      <div class="marca"><span class="gota">◐</span><strong>GlicoNutri</strong></div>

      <template v-if="!enviado">
        <h2>Recuperar acesso</h2>
        <p class="subtitulo">
          Informe o e-mail cadastrado. Enviaremos um link para você definir uma nova senha.
        </p>

        <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>

        <form @submit.prevent="enviar">
          <div class="campo">
            <label for="email">E-mail</label>
            <input id="email" v-model="email" type="email" autocomplete="username" required />
          </div>
          <button class="btn btn-primario largo" type="submit" :disabled="enviando || !email">
            {{ enviando ? 'Enviando…' : 'Enviar link' }}
          </button>
        </form>
      </template>

      <template v-else>
        <h2>Verifique seu e-mail</h2>
        <p class="subtitulo">
          Se houver conta ativa para <strong>{{ email }}</strong>, o link de redefinição
          foi enviado. Ele vale por 30 minutos e só pode ser usado uma vez.
        </p>
      </template>

      <RouterLink class="btn btn-secundario largo" :to="{ name: 'login' }">Voltar ao login</RouterLink>
    </div>
  </div>
</template>

<style scoped>
.tela { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: var(--md); }
.caixa { width: 100%; max-width: 400px; }
.marca { display: flex; align-items: center; gap: var(--xs); margin-bottom: var(--lg); }
.marca strong { font-size: 20px; }
.gota { color: var(--primary); font-size: 24px; }
.subtitulo { margin: var(--xs) 0 var(--lg); font-size: 14px; color: var(--text-secondary); }
.largo { width: 100%; margin-top: var(--xs); }
</style>
