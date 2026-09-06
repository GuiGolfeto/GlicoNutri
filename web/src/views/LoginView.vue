<script setup lang="ts">
import { computed, onUnmounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { mensagemDeErro } from '../api/client'
import type { LoginFalha } from '../api/tipos'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

const email = ref('')
const senha = ref('')
const erro = ref('')
const enviando = ref(false)

/** RN02 — a tela informa o tempo restante até o desbloqueio automático. */
const segundosBloqueio = ref(0)
let cronometro: ReturnType<typeof setInterval> | undefined

const bloqueado = computed(() => segundosBloqueio.value > 0)

const tempoRestante = computed(() => {
  const total = segundosBloqueio.value
  if (total >= 3600) {
    const horas = Math.floor(total / 3600)
    const minutos = Math.ceil((total % 3600) / 60)
    return `${horas}h${minutos > 0 ? ` ${minutos}min` : ''}`
  }
  if (total >= 60) return `${Math.ceil(total / 60)} min`
  return `${total}s`
})

function iniciarContagem(segundos: number) {
  segundosBloqueio.value = segundos
  clearInterval(cronometro)
  cronometro = setInterval(() => {
    segundosBloqueio.value -= 1
    if (segundosBloqueio.value <= 0) {
      clearInterval(cronometro)
      erro.value = ''
    }
  }, 1000)
}

onUnmounted(() => clearInterval(cronometro))

async function entrar() {
  erro.value = ''
  enviando.value = true
  try {
    const dados = await auth.entrar(email.value.trim(), senha.value)
    // RN03 — quem ainda usa a senha provisória vai direto para a troca.
    router.push({ name: dados.senhaProvisoria ? 'trocar-senha' : 'pacientes' })
  } catch (e) {
    const falha = (e as { response?: { data?: LoginFalha } }).response?.data
    erro.value = mensagemDeErro(e, 'Não foi possível entrar.')
    if (falha?.bloqueado && falha.segundosRestantes) iniciarContagem(falha.segundosRestantes)
  } finally {
    enviando.value = false
  }
}
</script>

<template>
  <div class="tela">
    <form class="card caixa" @submit.prevent="entrar">
      <div class="marca">
        <span class="gota">◐</span>
        <strong>GlicoNutri</strong>
      </div>
      <p class="subtitulo">Apoio ao controle nutricional do diabetes</p>

      <div v-if="erro" class="aviso" :class="bloqueado ? 'aviso-atencao' : 'aviso-erro'">
        {{ erro }}
        <template v-if="bloqueado">
          <br />Tente novamente em <strong class="numerico">{{ tempoRestante }}</strong>.
        </template>
      </div>

      <div class="campo">
        <label for="email">E-mail</label>
        <input id="email" v-model="email" type="email" autocomplete="username" required />
      </div>

      <div class="campo">
        <label for="senha">Senha</label>
        <input id="senha" v-model="senha" type="password" autocomplete="current-password" required />
      </div>

      <button class="btn btn-primario largo" type="submit" :disabled="enviando || bloqueado">
        {{ enviando ? 'Entrando…' : 'Entrar' }}
      </button>
    </form>
  </div>
</template>

<style scoped>
.tela { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: var(--md); }
.caixa { width: 100%; max-width: 400px; }
.marca { display: flex; align-items: center; gap: var(--xs); }
.marca strong { font-size: 22px; }
.gota { color: var(--primary); font-size: 26px; }
.subtitulo { margin: 4px 0 var(--lg); font-size: 14px; color: var(--text-secondary); }
.largo { width: 100%; margin-top: var(--xs); }
</style>
