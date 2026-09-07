import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: () => import('../views/LoginView.vue'), meta: { publica: true } },
    { path: '/recuperar-senha', name: 'recuperar-senha', component: () => import('../views/RecuperarSenhaView.vue'), meta: { publica: true } },
    { path: '/redefinir-senha', name: 'redefinir-senha', component: () => import('../views/RedefinirSenhaView.vue'), meta: { publica: true } },
    { path: '/trocar-senha', name: 'trocar-senha', component: () => import('../views/TrocarSenhaView.vue') },
    { path: '/', redirect: '/dashboard' },
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

router.beforeEach((para) => {
  const auth = useAuthStore()

  if (para.meta.publica) {
    return auth.autenticado && !auth.precisaTrocarSenha ? { name: 'dashboard' } : true
  }

  if (!auth.autenticado) return { name: 'login' }

  // RN03 — nenhuma outra tela abre antes de a senha provisória ser trocada.
  if (auth.precisaTrocarSenha && para.name !== 'trocar-senha') {
    return { name: 'trocar-senha' }
  }
  if (!auth.precisaTrocarSenha && para.name === 'trocar-senha') {
    return { name: 'dashboard' }
  }

  // RN04 — controle de acesso por perfil, espelhando as políticas da API.
  if (para.meta.administrador && !auth.ehAdministrador) return { name: 'dashboard' }
  if (para.meta.nutricionista && !auth.ehNutricionista) return { name: 'dashboard' }

  return true
})

export default router
