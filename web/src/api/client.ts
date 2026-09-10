import axios from 'axios'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5080/api',
  headers: { 'Content-Type': 'application/json' },
})

export const CHAVE_TOKEN = 'gliconutri.token'

api.interceptors.request.use((config) => {
  const token = localStorage.getItem(CHAVE_TOKEN)
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

/**
 * Um 401 significa token expirado ou inválido: derruba a sessão e devolve ao
 * login. O 403 não é tratado aqui — pode ser tanto falta de permissão (RN04)
 * quanto o bloqueio por senha provisória (RN03), e cada tela reage à sua maneira.
 */
api.interceptors.response.use(
  (resposta) => resposta,
  (erro) => {
    if (erro.response?.status === 401 && !erro.config?.url?.includes('/auth/login')) {
      localStorage.removeItem(CHAVE_TOKEN)
      if (location.pathname !== '/login') location.assign('/login')
    }
    return Promise.reject(erro)
  },
)

/** Extrai a mensagem que a API devolve em { mensagem } ou nos erros de validação. */
export function mensagemDeErro(erro: unknown, padrao = 'Não foi possível concluir a operação.'): string {
  const dados = (erro as { response?: { data?: unknown } })?.response?.data as
    | { mensagem?: string; errors?: Record<string, string[]> }
    | undefined

  if (dados?.mensagem) return dados.mensagem

  if (dados?.errors) {
    const primeiro = Object.values(dados.errors).flat()[0]
    if (primeiro) return primeiro
  }

  return padrao
}
