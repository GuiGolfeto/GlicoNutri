<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

function sair() {
  auth.sair()
  router.push({ name: 'login' })
}

const iniciais = (nome: string) =>
  nome.split(' ').filter(Boolean).slice(0, 2).map((p) => p[0]).join('').toUpperCase()
</script>

<template>
  <div class="app">
    <aside class="menu">
      <div class="marca">
        <span class="gota">◐</span>
        <strong>GlicoNutri</strong>
      </div>

      <nav v-if="!auth.ehNutricionista">
        <p class="secao">Meu acompanhamento</p>
        <RouterLink :to="{ name: 'minha-area' }">Início</RouterLink>
      </nav>

      <nav v-else>
        <p class="secao">Visão geral</p>
        <RouterLink :to="{ name: 'dashboard' }">Dashboard</RouterLink>

        <p class="secao">Pacientes</p>
        <RouterLink :to="{ name: 'pacientes' }">Lista de Pacientes</RouterLink>

        <p class="secao">Nutrição</p>
        <RouterLink :to="{ name: 'alimentos' }">Banco de Alimentos</RouterLink>
        <RouterLink :to="{ name: 'repositorio' }">Repositório Educativo</RouterLink>

        <!-- RN09 — só o Administrador gerencia nutricionistas. -->
        <template v-if="auth.ehAdministrador">
          <p class="secao">Administração</p>
          <RouterLink :to="{ name: 'nutricionistas' }">Nutricionistas</RouterLink>
        </template>
      </nav>

      <div class="usuario">
        <div class="avatar">{{ iniciais(auth.usuario?.nome ?? '?') }}</div>
        <div class="dados">
          <span class="nome">{{ auth.usuario?.nome }}</span>
          <span class="perfil">{{ auth.perfil }}</span>
        </div>
      </div>
      <button class="btn btn-secundario sair" @click="sair">Sair</button>
    </aside>

    <main class="conteudo">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.app { display: flex; min-height: 100vh; }

.menu {
  width: 240px;
  flex-shrink: 0;
  background: var(--surface);
  border-right: 1px solid var(--border);
  padding: var(--lg) var(--md);
  display: flex;
  flex-direction: column;
}

.marca { display: flex; align-items: center; gap: var(--xs); margin-bottom: var(--xl); padding: 0 var(--xs); }
.marca strong { font-size: 18px; }
.gota { color: var(--primary); font-size: 22px; }

nav { flex: 1; }

.secao {
  font-size: 12px;
  font-weight: 500;
  letter-spacing: 0.05em;
  text-transform: uppercase;
  color: var(--text-muted);
  margin: var(--lg) var(--xs) var(--xs);
}
.secao:first-child { margin-top: 0; }

nav a {
  display: block;
  padding: 9px 12px;
  border-radius: var(--raio);
  color: var(--text-secondary);
  font-size: 15px;
  text-decoration: none;
}
nav a:hover { background: var(--hover); text-decoration: none; }
nav a.router-link-active { background: var(--teal-light); color: var(--primary); font-weight: 500; }

.usuario {
  display: flex;
  align-items: center;
  gap: var(--xs);
  padding: var(--sm) var(--xs);
  border-top: 1px solid var(--border);
  margin-top: var(--md);
}

.avatar {
  width: 34px; height: 34px;
  flex-shrink: 0;
  border-radius: 50%;
  background: var(--teal-light);
  color: var(--primary);
  font-size: 13px;
  font-weight: 600;
  display: flex; align-items: center; justify-content: center;
}

.dados { display: flex; flex-direction: column; min-width: 0; }
.nome { font-size: 14px; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.perfil { font-size: 11px; letter-spacing: 0.05em; color: var(--text-muted); }

.sair { width: 100%; margin-top: var(--xs); }

.conteudo { flex: 1; padding: var(--xl); max-width: 1280px; min-width: 0; }
@media (max-width: 720px) { .conteudo { padding: var(--md); } }
</style>
