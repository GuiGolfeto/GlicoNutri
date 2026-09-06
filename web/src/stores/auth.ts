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
    ehNutricionista, ehAdministrador, entrar, alterarSenha, sair,
  }
})
