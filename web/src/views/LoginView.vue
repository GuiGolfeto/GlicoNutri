<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { mensagemDeErro } from '../api/client'
import type { LoginFalha } from '../api/tipos'
import { googleHabilitado, renderizarBotaoGoogle } from '../api/google'
import LogoGlicoNutri from '../components/LogoGlicoNutri.vue'
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
    <!-- Painel de apresentação: a tela de entrada é a primeira impressão de quem
         usa o sistema, e antes dela havia só um cartão solto no meio da página. -->
    <aside class="apresentacao">
      <RouterLink :to="{ name: 'divulgacao' }" class="marca-clara">
        <LogoGlicoNutri :tamanho="30" />
        <strong>GlicoNutri</strong>
      </RouterLink>

      <div class="discurso">
        <h1>O acompanhamento nutricional do diabetes, com o histórico à vista.</h1>
        <p>
          Glicemia, medidas, plano alimentar e como o paciente se sente — reunidos para a
          decisão clínica acontecer com dado, não com memória.
        </p>
      </div>

      <div class="cartao-vitrine">
        <div class="linha-vitrine">
          <span class="apoio-claro">Média glicêmica · 7 dias</span>
          <span class="selo-claro">No alvo</span>
        </div>
        <strong class="numerico numero-grande">104 mg/dL</strong>
        <div class="barras">
          <span v-for="(h, i) in [42, 58, 74, 96, 63, 48, 57]" :key="i" :style="{ height: h + '%' }" />
        </div>
      </div>

      <p class="rodape-apresentacao">
        Em parceria com a ADJ — Associação de Diabetes Juvenil de Birigui
      </p>
    </aside>

    <div class="lado-formulario">
      <form class="caixa" @submit.prevent="entrar">
        <div class="marca-compacta">
          <LogoGlicoNutri :tamanho="26" />
          <strong>GlicoNutri</strong>
        </div>

        <h2>Entrar no sistema</h2>
        <p class="subtitulo">Use as credenciais que a equipe cadastrou para você.</p>

        <div v-if="erro" class="aviso" :class="bloqueado ? 'aviso-atencao' : 'aviso-erro'">
          {{ erro }}
          <template v-if="bloqueado">
            <br />Tente novamente em <strong class="numerico">{{ tempoRestante }}</strong>.
          </template>
        </div>

        <div class="campo">
          <label for="email">E-mail</label>
          <input id="email" v-model="email" type="email" autocomplete="username" required
                 placeholder="voce@exemplo.com" />
        </div>

        <div class="campo">
          <div class="rotulo-senha">
            <label for="senha">Senha</label>
            <RouterLink class="esqueci" :to="{ name: 'recuperar-senha' }">Esqueci minha senha</RouterLink>
          </div>
          <input id="senha" v-model="senha" type="password" autocomplete="current-password" required
                 placeholder="••••••••" />
        </div>

        <button class="btn btn-primario largo" type="submit" :disabled="enviando || bloqueado">
          {{ enviando ? 'Entrando…' : 'Entrar' }}
        </button>

        <!-- RN01 — login federado, quando configurado neste ambiente -->
        <template v-if="googleHabilitado">
          <div class="separador"><span>ou</span></div>
          <div ref="caixaGoogle" class="google" />
          <p class="ajuda-google">
            Só funciona para e-mails já cadastrados pelo administrador da ADJ.
          </p>
        </template>

        <p class="sem-cadastro">
          Não tem acesso? O cadastro é feito pela equipe —
          <RouterLink :to="{ name: 'divulgacao' }">conheça o sistema</RouterLink>.
        </p>
      </form>
    </div>
  </div>
</template>

<style scoped>
.tela { min-height: 100vh; display: grid; grid-template-columns: 1.05fr 1fr; }

/* ── Lado da apresentação ────────────────────────────────────────────────── */
.apresentacao {
  background: linear-gradient(160deg, var(--primary) 0%, #004a42 100%);
  color: #fff;
  padding: var(--xl) 48px;
  display: flex;
  flex-direction: column;
  gap: var(--xl);
}
.marca-clara { display: flex; align-items: center; gap: var(--xs); color: #fff; text-decoration: none; }
.marca-clara strong { font-size: 20px; }
.marca-clara:hover { text-decoration: none; }

.discurso { margin-top: auto; }
.discurso h1 { color: #fff; font-size: 34px; line-height: 44px; letter-spacing: -0.02em; }
.discurso p { color: rgba(255, 255, 255, 0.82); font-size: 16px; margin-top: var(--md); max-width: 460px; }

.cartao-vitrine {
  background: rgba(255, 255, 255, 0.1);
  border: 1px solid rgba(255, 255, 255, 0.16);
  border-radius: var(--raio-card);
  padding: var(--lg);
  max-width: 420px;
}
.linha-vitrine { display: flex; align-items: center; justify-content: space-between; gap: var(--sm); }
.apoio-claro { font-size: 13px; color: rgba(255, 255, 255, 0.75); }
.selo-claro {
  font-size: 12px; font-weight: 500; padding: 3px 10px; border-radius: 999px;
  background: rgba(255, 255, 255, 0.16); color: #fff;
}
.numero-grande { display: block; font-size: 30px; font-weight: 600; margin: var(--xs) 0 var(--md); }
.barras { display: flex; align-items: flex-end; gap: 6px; height: 64px; }
.barras span { flex: 1; background: rgba(255, 255, 255, 0.45); border-radius: 3px 3px 0 0; }
.barras span:nth-child(4) { background: #fff; }

.rodape-apresentacao { margin: auto 0 0; font-size: 13px; color: rgba(255, 255, 255, 0.7); }

/* ── Lado do formulário ──────────────────────────────────────────────────── */
.lado-formulario {
  display: flex; align-items: center; justify-content: center;
  padding: var(--xl) var(--lg); background: var(--surface);
}
.caixa { width: 100%; max-width: 380px; }

.marca-compacta { display: none; align-items: center; gap: var(--xs); margin-bottom: var(--lg); }
.marca-compacta strong { font-size: 20px; }

.caixa h2 { font-size: 26px; }
.subtitulo { margin: 6px 0 var(--lg); font-size: 15px; color: var(--text-secondary); }

.rotulo-senha { display: flex; align-items: baseline; justify-content: space-between; gap: var(--sm); }
.esqueci { font-size: 13px; }
.largo { width: 100%; margin-top: var(--xs); }

.separador {
  display: flex; align-items: center; gap: var(--sm);
  margin: var(--lg) 0 var(--md); color: var(--text-muted); font-size: 13px;
}
.separador::before, .separador::after { content: ''; flex: 1; height: 1px; background: var(--border); }
.google { display: flex; justify-content: center; }
.ajuda-google { font-size: 12px; color: var(--text-muted); text-align: center; margin: var(--xs) 0 0; }

.sem-cadastro {
  margin-top: var(--xl); padding-top: var(--md); border-top: 1px solid var(--border);
  font-size: 13px; color: var(--text-muted); text-align: center;
}

@media (max-width: 900px) {
  .tela { grid-template-columns: 1fr; }
  .apresentacao { display: none; }
  .marca-compacta { display: flex; }
}
</style>
