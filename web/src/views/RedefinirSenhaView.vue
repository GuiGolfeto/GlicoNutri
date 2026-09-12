<script setup lang="ts">
import LogoGlicoNutri from '../components/LogoGlicoNutri.vue'
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api, mensagemDeErro } from '../api/client'

const rota = useRoute()
const router = useRouter()

const token = String(rota.query.token ?? '')
const novaSenha = ref('')
const confirmacao = ref('')
const enviando = ref(false)
const erro = ref('')
const concluido = ref(false)

/** RN02 do UC001 — 8 caracteres com letras e números. */
const curta = computed(() => novaSenha.value.length > 0 && novaSenha.value.length < 8)
const semLetra = computed(() => novaSenha.value.length > 0 && !/[a-zA-Z]/.test(novaSenha.value))
const semNumero = computed(() => novaSenha.value.length > 0 && !/[0-9]/.test(novaSenha.value))
const naoConfere = computed(() => confirmacao.value.length > 0 && novaSenha.value !== confirmacao.value)

const valida = computed(() =>
  novaSenha.value.length >= 8 && !semLetra.value && !semNumero.value && !naoConfere.value)

async function redefinir() {
  erro.value = ''
  enviando.value = true
  try {
    await api.post('/auth/redefinir-senha', { token, novaSenha: novaSenha.value })
    concluido.value = true
    setTimeout(() => router.push({ name: 'login' }), 2500)
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
      <div class="marca"><LogoGlicoNutri :tamanho="24" /><strong>GlicoNutri</strong></div>

      <div v-if="!token" class="aviso aviso-erro">
        Link inválido. Solicite uma nova redefinição de senha.
      </div>

      <template v-else-if="concluido">
        <h2>Senha redefinida</h2>
        <p class="subtitulo">Redirecionando para o login…</p>
      </template>

      <template v-else>
        <h2>Definir nova senha</h2>
        <p class="subtitulo">Mínimo de 8 caracteres, com letras e números.</p>

        <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>

        <form @submit.prevent="redefinir">
          <div class="campo">
            <label for="nova">Nova senha</label>
            <input id="nova" v-model="novaSenha" type="password" autocomplete="new-password" required />
            <span v-if="curta" class="erro-campo">A senha deve ter ao menos 8 caracteres.</span>
            <span v-else-if="semLetra" class="erro-campo">A senha deve conter ao menos uma letra.</span>
            <span v-else-if="semNumero" class="erro-campo">A senha deve conter ao menos um número.</span>
          </div>

          <div class="campo">
            <label for="confirma">Confirme a nova senha</label>
            <input id="confirma" v-model="confirmacao" type="password" autocomplete="new-password" required />
            <span v-if="naoConfere" class="erro-campo">As senhas não coincidem.</span>
          </div>

          <button class="btn btn-primario largo" type="submit" :disabled="enviando || !valida">
            {{ enviando ? 'Salvando…' : 'Redefinir senha' }}
          </button>
        </form>
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
.subtitulo { margin: var(--xs) 0 var(--lg); font-size: 14px; color: var(--text-secondary); }
.largo { width: 100%; margin-top: var(--xs); }
</style>
