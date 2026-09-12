<script setup lang="ts">
/**
 * Marca do GlicoNutri: gota com check, em #00897B.
 *
 * O que estava no sistema era um círculo meio preenchido, que não é a marca do
 * projeto. Ficar num componente só evita que a próxima tela repita o engano.
 */
withDefaults(defineProps<{ tamanho?: number }>(), { tamanho: 24 })
</script>

<template>
  <svg
    :width="tamanho"
    :height="tamanho"
    viewBox="0 0 40 40"
    fill="none"
    role="img"
    aria-label="GlicoNutri"
    class="logo"
  >
    <path
      d="M20 38C27.732 38 34 31.732 34 24C34 16.268 20 2 20 2C20 2 6 16.268 6 24C6 31.732 12.268 38 20 38Z"
      fill="#00897B"
    />
    <path
      d="M15 24L18.5 27.5L25.5 20.5"
      stroke="white"
      stroke-width="3"
      stroke-linecap="round"
      stroke-linejoin="round"
    />
  </svg>
</template>

<style scoped>
.logo { flex-shrink: 0; display: block; }
</style>
