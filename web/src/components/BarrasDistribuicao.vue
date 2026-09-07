<script setup lang="ts">
import { computed } from 'vue'
import type { FatiaDistribuicao } from '../api/tipos'

const props = defineProps<{ fatias: FatiaDistribuicao[]; cores?: string[] }>()

const total = computed(() => props.fatias.reduce((s, f) => s + f.quantidade, 0))

const paleta = ['var(--success)', 'var(--warning)', 'var(--danger)', 'var(--text-muted)']
const cor = (i: number) => props.cores?.[i] ?? paleta[i % paleta.length]

const percentual = (q: number) => (total.value === 0 ? 0 : (q / total.value) * 100)
</script>

<template>
  <div v-if="total === 0" class="vazio-barras">Sem dados.</div>

  <div v-else class="distribuicao">
    <div class="trilha">
      <div v-for="(f, i) in fatias.filter(x => x.quantidade > 0)" :key="f.rotulo"
           class="fatia" :style="{ width: `${percentual(f.quantidade)}%`, background: cor(i) }"
           :title="`${f.rotulo}: ${f.quantidade}`" />
    </div>

    <ul class="legenda">
      <li v-for="(f, i) in fatias" :key="f.rotulo">
        <span class="marca" :style="{ background: cor(i) }" />
        <span class="rotulo">{{ f.rotulo }}</span>
        <strong class="numerico">{{ f.quantidade }}</strong>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.trilha {
  display: flex; height: 10px; border-radius: 9999px; overflow: hidden;
  background: var(--hover); margin-bottom: var(--sm);
}
.fatia { transition: width 0.3s; }

.legenda { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 6px; }
.legenda li { display: flex; align-items: center; gap: var(--xs); font-size: 14px; }
.marca { width: 9px; height: 9px; border-radius: 2px; flex-shrink: 0; }
.rotulo { flex: 1; color: var(--text-secondary); }

.vazio-barras { color: var(--text-muted); font-size: 14px; padding: var(--md) 0; }
</style>
