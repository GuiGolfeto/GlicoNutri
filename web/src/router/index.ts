import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    // Páginas de divulgação, abertas a quem ainda não entrou. Diferente de
    // 'publica', não expulsam quem já está autenticado: dá para abrir a
    // apresentação do sistema com a sessão em curso.
    { path: '/', name: 'divulgacao', component: () => import('../views/DivulgacaoView.vue'), meta: { divulgacao: true } },
    { path: '/funcionalidades', name: 'funcionalidades', component: () => import('../views/FuncionalidadesView.vue'), meta: { divulgacao: true } },
    { path: '/sobre', name: 'sobre', component: () => import('../views/SobreView.vue'), meta: { divulgacao: true } },

    { path: '/login', name: 'login', component: () => import('../views/LoginView.vue'), meta: { publica: true } },
    { path: '/recuperar-senha', name: 'recuperar-senha', component: () => import('../views/RecuperarSenhaView.vue'), meta: { publica: true } },
    { path: '/redefinir-senha', name: 'redefinir-senha', component: () => import('../views/RedefinirSenhaView.vue'), meta: { publica: true } },
    { path: '/trocar-senha', name: 'trocar-senha', component: () => import('../views/TrocarSenhaView.vue') },
    { path: '/inicio', redirect: () => ({ name: telaInicial() }) },
    { path: '/minha-area', name: 'minha-area', component: () => import('../views/MinhaAreaView.vue'), meta: { paciente: true } },
    { path: '/meus-favoritos', name: 'meus-favoritos', component: () => import('../views/FavoritosView.vue'), meta: { paciente: true } },
    { path: '/dashboard', name: 'dashboard', component: () => import('../views/DashboardView.vue'), meta: { nutricionista: true } },
    { path: '/pacientes', name: 'pacientes', component: () => import('../views/PacientesView.vue'), meta: { nutricionista: true } },
    { path: '/pacientes/novo', name: 'paciente-novo', component: () => import('../views/PacienteFormView.vue'), meta: { nutricionista: true } },
    { path: '/pacientes/:id', name: 'paciente-detalhe', component: () => import('../views/PacienteDetalheView.vue'), meta: { nutricionista: true } },
    { path: '/pacientes/:id/editar', name: 'paciente-editar', component: () => import('../views/PacienteEditarView.vue'), meta: { nutricionista: true } },
    { path: '/alimentos', name: 'alimentos', component: () => import('../views/AlimentosView.vue'), meta: { nutricionista: true } },
    { path: '/repositorio', name: 'repositorio', component: () => import('../views/RepositorioView.vue'), meta: { nutricionista: true } },
    { path: '/nutricionistas', name: 'nutricionistas', component: () => import('../views/NutricionistasView.vue'), meta: { administrador: true } },
    { path: '/nutricionistas/:id/editar', name: 'nutricionista-editar', component: () => import('../views/NutricionistaEditarView.vue'), meta: { administrador: true } },
    { path: '/nutricionistas/novo', name: 'nutricionista-novo', component: () => import('../views/NutricionistaFormView.vue'), meta: { administrador: true } },
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

  if (para.meta.divulgacao) return true

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
