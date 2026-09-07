<script setup lang="ts">
import { computed } from 'vue'

export interface PontoGrafico {
  data: string
  valor: number
  destaque?: boolean
  rotulo?: string
}

const props = withDefaults(defineProps<{
  pontos: PontoGrafico[]
  /** Faixa alvo desenhada como banda de fundo. */
  minAlvo?: number | null
  maxAlvo?: number | null
  unidade?: string
  altura?: number
  cor?: string
}>(), { altura: 200, cor: 'var(--primary)', unidade: '' })

const largura = 720
const margem = { topo: 12, direita: 12, base: 26, esquerda: 40 }

const area = computed(() => ({
  w: largura - margem.esquerda - margem.direita,
  h: props.altura - margem.topo - margem.base,
}))

/** Escala com folga de 10%, sempre incluindo a faixa alvo quando existir. */
const escala = computed(() => {
  const valores = props.pontos.map((p) => p.valor)
  if (props.minAlvo != null) valores.push(props.minAlvo)
  if (props.maxAlvo != null) valores.push(props.maxAlvo)
  if (valores.length === 0) return { min: 0, max: 1 }

  const min = Math.min(...valores)
  const max = Math.max(...valores)
  const folga = (max - min || max || 1) * 0.1

  return { min: Math.max(0, min - folga), max: max + folga }
})

const x = (i: number) =>
  margem.esquerda + (props.pontos.length <= 1
    ? area.value.w / 2
    : (i / (props.pontos.length - 1)) * area.value.w)

const y = (valor: number) => {
  const { min, max } = escala.value
  const proporcao = (valor - min) / (max - min || 1)
  return margem.topo + area.value.h - proporcao * area.value.h
}

const linha = computed(() =>
  props.pontos.map((p, i) => `${i === 0 ? 'M' : 'L'} ${x(i).toFixed(1)} ${y(p.valor).toFixed(1)}`).join(' '))

/** Banda da faixa alvo: o que está dentro dela é o que se quer ver. */
const banda = computed(() => {
  if (props.minAlvo == null || props.maxAlvo == null) return null
  const topo = y(props.maxAlvo)
  return { y: topo, altura: Math.max(0, y(props.minAlvo) - topo) }
})

const marcas = computed(() => {
  const { min, max } = escala.value
  return [0, 0.25, 0.5, 0.75, 1].map((f) => {
    const valor = min + (max - min) * f
    return { valor: Math.round(valor), y: y(valor) }
  })
})

const dataCurta = (iso: string) =>
  new Date(iso).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })

/** Rótulos do eixo horizontal, no máximo 6 para não embolar. */
const rotulosX = computed(() => {
  const total = props.pontos.length
  if (total === 0) return []
  const passo = Math.max(1, Math.ceil(total / 6))
  return props.pontos
    .map((p, i) => ({ i, texto: dataCurta(p.data) }))
    .filter((_, i) => i % passo === 0 || i === total - 1)
})
</script>

<template>
  <div v-if="pontos.length === 0" class="vazio-grafico">Sem dados no período.</div>

  <svg v-else class="grafico" :viewBox="`0 0 ${largura} ${altura}`" preserveAspectRatio="none"
       role="img" :aria-label="`Gráfico com ${pontos.length} pontos`">
    <rect v-if="banda" :x="margem.esquerda" :y="banda.y" :width="area.w" :height="banda.altura"
          class="banda" />

    <g class="grade">
      <line v-for="m in marcas" :key="m.valor" :x1="margem.esquerda" :y1="m.y"
            :x2="largura - margem.direita" :y2="m.y" />
      <text v-for="m in marcas" :key="`t${m.valor}`" :x="margem.esquerda - 6" :y="m.y + 3"
            text-anchor="end">{{ m.valor }}</text>
    </g>

    <path :d="linha" class="serie" :style="{ stroke: cor }" />

    <circle v-for="(p, i) in pontos" :key="i" :cx="x(i)" :cy="y(p.valor)" r="3.5"
            :class="['ponto', { fora: p.destaque }]" :style="{ fill: p.destaque ? undefined : cor }">
      <title>{{ dataCurta(p.data) }} — {{ p.valor }} {{ unidade }}{{ p.rotulo ? ` · ${p.rotulo}` : '' }}</title>
    </circle>

    <text v-for="r in rotulosX" :key="`x${r.i}`" :x="x(r.i)" :y="altura - 8"
          text-anchor="middle" class="eixo-x">{{ r.texto }}</text>
  </svg>
</template>

<style scoped>
.grafico { width: 100%; height: auto; overflow: visible; }

.banda { fill: var(--teal-light); opacity: 0.55; }

.grade line { stroke: var(--border); stroke-width: 1; }
.grade text, .eixo-x { font-size: 9px; fill: var(--text-muted); font-variant-numeric: tabular-nums; }

.serie { fill: none; stroke-width: 2; stroke-linejoin: round; stroke-linecap: round; }

.ponto { stroke: var(--surface); stroke-width: 1.5; }
.ponto.fora { fill: var(--danger); }

.vazio-grafico {
  display: flex; align-items: center; justify-content: center;
  height: 160px; color: var(--text-muted); font-size: 14px;
  background: var(--background); border-radius: var(--raio);
}
</style>
