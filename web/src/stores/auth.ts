import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { api, CHAVE_TOKEN } from '../api/client'
import type { LoginResponse, Perfil } from '../api/tipos'

const CHAVE_USUARIO = 'gliconutri.usuario'

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem(CHAVE_TOKEN))
  const usuario = ref<LoginResponse | null>(
    JSON.parse(localStorage.getItem(CHAVE_USUARIO) ?? 'null'),
  )

  const autenticado = computed(() => !!token.value && !!usuario.value)
  const perfil = computed<Perfil | null>(() => usuario.value?.perfil ?? null)

  /** RN03 — enquanto verdadeiro, a única tela permitida é a troca de senha. */
  const precisaTrocarSenha = computed(() => usuario.value?.senhaProvisoria === true)

  /** RN04 — o Administrador herda tudo que o Nutricionista pode fazer. */
  const ehNutricionista = computed(
    () => perfil.value === 'NUTRICIONISTA' || perfil.value === 'ADMINISTRADOR',
  )
  const ehAdministrador = computed(() => perfil.value === 'ADMINISTRADOR')

  function guardar(dados: LoginResponse) {
    token.value = dados.token
    usuario.value = dados
    localStorage.setItem(CHAVE_TOKEN, dados.token)
    localStorage.setItem(CHAVE_USUARIO, JSON.stringify(dados))
  }

  async function entrar(email: string, senha: string) {
    const { data } = await api.post<LoginResponse>('/auth/login', { email, senha })
    guardar(data)
    return data
  }

  /** RN01 — troca o ID token do Google por uma credencial do próprio sistema. */
  async function entrarComGoogle(idToken: string) {
    const { data } = await api.post<LoginResponse>('/auth/login-google', { idToken })
    guardar(data)
    return data
  }

  /**
   * RN03 do UC001 — renova o token em uso ativo. Sem isto a sessão morre 24h
   * depois do login, e o nutricionista perde o que estava preenchendo.
   */
  async function renovar() {
    if (!token.value) return
    try {
      const { data } = await api.post<LoginResponse>('/auth/renovar')
      guardar(data)
    } catch {
      // Token já expirado ou conta desativada: o interceptador de 401 cuida.
    }
  }

  /** Minutos antes da expiração em que vale a pena renovar. */
  const MARGEM_MINUTOS = 60

  /** Renova quando falta pouco, e ao voltar para a aba após um tempo parado. */
  function iniciarRenovacaoAutomatica() {
    const precisaRenovar = () => {
      if (!usuario.value?.expiraEm) return false
      const restante = new Date(usuario.value.expiraEm).getTime() - Date.now()
      return restante < MARGEM_MINUTOS * 60_000
    }

    const verificar = () => { if (autenticado.value && precisaRenovar()) renovar() }

    setInterval(verificar, 5 * 60_000)
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') verificar()
    })
  }

  async function alterarSenha(senhaAtual: string, novaSenha: string) {
    await api.post('/auth/alterar-senha', { senhaAtual, novaSenha })
    // O token em mãos ainda carrega senha_provisoria=true; reautenticar com a
    // senha nova é o que devolve uma credencial sem a restrição da RN03.
    if (usuario.value) await entrar(usuario.value.email, novaSenha)
  }

  function sair() {
    token.value = null
    usuario.value = null
    localStorage.removeItem(CHAVE_TOKEN)
    localStorage.removeItem(CHAVE_USUARIO)
  }

  return {
    token, usuario, autenticado, perfil, precisaTrocarSenha,
    ehNutricionista, ehAdministrador, entrar, entrarComGoogle, alterarSenha, sair,
    renovar, iniciarRenovacaoAutomatica,
  }
})
