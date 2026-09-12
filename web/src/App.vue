<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import AppLayout from './layouts/AppLayout.vue'
import PublicoLayout from './layouts/PublicoLayout.vue'
import { useAuthStore } from './stores/auth'

const rota = useRoute()
const auth = useAuthStore()

// Login e troca de senha ficam fora do layout: nas duas o usuário ainda não
// tem acesso ao menu.
// UC001 RN03 — mantém a credencial válida enquanto o sistema estiver em uso.
onMounted(() => auth.iniciarRenovacaoAutomatica())

// As páginas de divulgação têm moldura própria, com o menu público.
const ehDivulgacao = computed(() => rota.meta.divulgacao === true)

const semLayout = computed(
  () => rota.meta.publica === true || rota.name === 'trocar-senha' || !auth.autenticado,
)
</script>

<template>
  <PublicoLayout v-if="ehDivulgacao">
    <RouterView />
  </PublicoLayout>
  <RouterView v-else-if="semLayout" />
  <AppLayout v-else>
    <RouterView />
  </AppLayout>
</template>
