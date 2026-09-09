<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { mensagemDeErro } from '../api/client'
import type { LoginFalha } from '../api/tipos'
import { googleHabilitado, renderizarBotaoGoogle } from '../api/google'
import { useAuthStore } from '../stores/auth'
import type { Perfil } from '../api/tipos'

/** O paciente não tem telas de gestão; sua área é outra. */
const destinoPorPerfil = (perfil: Perfil) =>
  perfil === 'PACIENTE' ? 'minha-area' : 'dashboard'

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

const caixaGoogle = ref<HTMLElement | null>(null)

onMounted(async () => {
  if (!googleHabilitado || !caixaGoogle.value) return
  try {
    await renderizarBotaoGoogle(caixaGoogle.value, entrarComGoogle)
  } catch {
    // O login por senha continua disponível; o botão apenas não aparece.
  }
})

async function entrarComGoogle(idToken: string) {
  erro.value = ''
  enviando.value = true
  try {
    const dados = await auth.entrarComGoogle(idToken)
    router.push({ name: dados.senhaProvisoria ? 'trocar-senha' : destinoPorPerfil(dados.perfil) })
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível entrar com o Google.')
  } finally {
    enviando.value = false
  }
}

async function entrar() {
  erro.value = ''
  enviando.value = true
  try {
    const dados = await auth.entrar(email.value.trim(), senha.value)
    // RN03 — quem ainda usa a senha provisória vai direto para a troca.
    router.push({ name: dados.senhaProvisoria ? 'trocar-senha' : destinoPorPerfil(dados.perfil) })
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

      <RouterLink class="esqueci" :to="{ name: 'recuperar-senha' }">Esqueci minha senha</RouterLink>

      <!-- RN01 — login federado, quando configurado neste ambiente -->
      <template v-if="googleHabilitado">
        <div class="separador"><span>ou</span></div>
        <div ref="caixaGoogle" class="google" />
        <p class="ajuda-google">
          Só funciona para e-mails já cadastrados pelo administrador da ADJ.
        </p>
      </template>
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
.esqueci { display: block; text-align: center; margin-top: var(--md); font-size: 14px; }

.separador {
  display: flex; align-items: center; gap: var(--sm);
  margin: var(--lg) 0 var(--md); color: var(--text-muted); font-size: 13px;
}
.separador::before, .separador::after {
  content: ''; flex: 1; height: 1px; background: var(--border);
}
.google { display: flex; justify-content: center; }
.ajuda-google { font-size: 12px; color: var(--text-muted); text-align: center; margin: var(--xs) 0 0; }
</style>
