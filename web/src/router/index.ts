import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: () => import('../views/LoginView.vue'), meta: { publica: true } },
    { path: '/recuperar-senha', name: 'recuperar-senha', component: () => import('../views/RecuperarSenhaView.vue'), meta: { publica: true } },
    { path: '/redefinir-senha', name: 'redefinir-senha', component: () => import('../views/RedefinirSenhaView.vue'), meta: { publica: true } },
    { path: '/trocar-senha', name: 'trocar-senha', component: () => import('../views/TrocarSenhaView.vue') },
    { path: '/', redirect: () => ({ name: telaInicial() }) },
    { path: '/minha-area', name: 'minha-area', component: () => import('../views/MinhaAreaView.vue'), meta: { paciente: true } },
    { path: '/dashboard', name: 'dashboard', component: () => import('../views/DashboardView.vue'), meta: { nutricionista: true } },
  ],
})

/**
 * Tela inicial de cada perfil. O paciente não tem acesso a nenhuma tela de
 * gestão: mandá-lo ao dashboard criava um laço de redirecionamento, porque a
 * própria guarda o expulsava de volta para lá.
 */
function telaInicial(): string {
  const auth = useAuthStore()
  return auth.ehNutricionista ? 'dashboard' : 'minha-area'
}

router.beforeEach((para) => {
  const auth = useAuthStore()

  if (para.meta.publica) {
    return auth.autenticado && !auth.precisaTrocarSenha ? { name: telaInicial() } : true
  }

  if (!auth.autenticado) return { name: 'login' }

  // RN03 — nenhuma outra tela abre antes de a senha provisória ser trocada.
  if (auth.precisaTrocarSenha && para.name !== 'trocar-senha') {
    return { name: 'trocar-senha' }
  }
  if (!auth.precisaTrocarSenha && para.name === 'trocar-senha') {
    return { name: telaInicial() }
  }

  // RN04 — controle de acesso por perfil, espelhando as políticas da API.
  // O destino do desvio é sempre a tela inicial do perfil de quem está logado,
  // nunca uma tela que ele também não pode ver.
  if (para.meta.administrador && !auth.ehAdministrador) return { name: telaInicial() }
  if (para.meta.nutricionista && !auth.ehNutricionista) return { name: telaInicial() }
  if (para.meta.paciente && auth.ehNutricionista) return { name: 'dashboard' }

  return true
})

export default router
