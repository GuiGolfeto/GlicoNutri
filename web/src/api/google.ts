/**
 * RN01 — login federado com conta Google no sistema web.
 *
 * Carrega o Google Identity Services sob demanda e devolve o ID token, que a
 * API valida. Sem VITE_GOOGLE_CLIENT_ID configurado o botão nem aparece: o
 * login por senha continua sendo o caminho principal.
 */

const CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined

export const googleHabilitado = !!CLIENT_ID

let carregando: Promise<void> | null = null

function carregarScript(): Promise<void> {
  if (carregando) return carregando

  carregando = new Promise((resolve, reject) => {
    if (document.getElementById('gis-script')) return resolve()

    const script = document.createElement('script')
    script.id = 'gis-script'
    script.src = 'https://accounts.google.com/gsi/client'
    script.async = true
    script.onload = () => resolve()
    script.onerror = () => reject(new Error('Não foi possível carregar o login do Google.'))
    document.head.appendChild(script)
  })

  return carregando
}

interface CredencialGoogle { credential: string }

/** Renderiza o botão oficial do Google dentro do elemento indicado. */
export async function renderizarBotaoGoogle(
  destino: HTMLElement,
  aoAutenticar: (idToken: string) => void,
): Promise<void> {
  if (!CLIENT_ID) return

  await carregarScript()

  const google = (window as unknown as { google?: any }).google
  if (!google?.accounts?.id) throw new Error('Login do Google indisponível.')

  google.accounts.id.initialize({
    client_id: CLIENT_ID,
    callback: (resposta: CredencialGoogle) => aoAutenticar(resposta.credential),
  })

  google.accounts.id.renderButton(destino, {
    theme: 'outline',
    size: 'large',
    text: 'signin_with',
    locale: 'pt-BR',
    width: destino.clientWidth || 320,
  })
}
