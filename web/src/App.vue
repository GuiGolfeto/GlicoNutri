<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import AppLayout from './layouts/AppLayout.vue'
import { useAuthStore } from './stores/auth'

const rota = useRoute()
const auth = useAuthStore()

// Login e troca de senha ficam fora do layout: nas duas o usuário ainda não
// tem acesso ao menu.
// UC001 RN03 — mantém a credencial válida enquanto o sistema estiver em uso.
onMounted(() => auth.iniciarRenovacaoAutomatica())

const semLayout = computed(
  () => rota.meta.publica === true || rota.name === 'trocar-senha' || !auth.autenticado,
)
</script>

<template>
  <RouterView v-if="semLayout" />
  <AppLayout v-else>
    <RouterView />
  </AppLayout>
</template>
