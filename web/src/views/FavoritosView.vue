<script setup lang="ts">
/**
 * RF09.3 — o que o paciente guardou, reunido em um lugar.
 *
 * O coração espalhado pelas listas marca; esta tela é onde o material guardado
 * fica acessível depois. Material despublicado pelo administrador (RN29) some
 * daqui sem apagar a preferência: volta sozinho se for republicado.
 */
import { computed, onMounted, ref } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type { Favoritos } from '../api/tipos'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const id = computed(() => auth.usuario?.usuarioId ?? 0)

const favoritos = ref<Favoritos>({ conteudos: [], receitas: [] })
const carregando = ref(true)
const erro = ref('')

const vazio = computed(
  () => favoritos.value.conteudos.length === 0 && favoritos.value.receitas.length === 0,
)

async function carregar() {
  carregando.value = true
  try {
    const { data } = await api.get<Favoritos>(`/pacientes/${id.value}/favoritos`)
    favoritos.value = data
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar seus favoritos.')
  } finally {
    carregando.value = false
  }
}

/** Remover daqui tira da lista na hora; se a chamada falhar, o item volta. */
async function remover(tipo: 'conteudos' | 'receitas', idItem: number) {
  const anterior = favoritos.value

  favoritos.value = tipo === 'conteudos'
    ? { ...anterior, conteudos: anterior.conteudos.filter((c) => c.conteudoId !== idItem) }
    : { ...anterior, receitas: anterior.receitas.filter((r) => r.receitaId !== idItem) }

  try {
    await api.delete(`/pacientes/${id.value}/favoritos/${tipo}/${idItem}`)
  } catch (e) {
    favoritos.value = anterior
    erro.value = mensagemDeErro(e, 'Não foi possível remover dos favoritos.')
  }
}

function idDoYouTube(url: string): string | null {
  try {
    const endereco = new URL(url)
    if (endereco.hostname.endsWith('youtu.be')) return endereco.pathname.slice(1) || null
    if (!endereco.hostname.endsWith('youtube.com')) return null
    if (endereco.pathname.startsWith('/embed/')) return endereco.pathname.slice(7) || null
    return endereco.searchParams.get('v')
  } catch {
    return null
  }
}

const dominioDe = (url: string) => {
  try {
    return new URL(url).hostname.replace(/^www\./, '')
  } catch {
    return url
  }
}

const linkSeguro = (url: string | null) =>
  !!url && (url.startsWith('https://') || url.startsWith('http://'))

const data = (iso: string) => new Date(iso).toLocaleDateString('pt-BR')

onMounted(carregar)
</script>

<template>
  <div class="cabecalho">
    <h1>Meus favoritos</h1>
    <p class="sub">O material que você guardou para ler ou preparar depois.</p>
  </div>

  <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>
  <p v-if="carregando" class="carregando">Carregando…</p>

  <template v-else>
    <section v-if="vazio" class="card vazio">
      <p class="titulo-vazio">Você ainda não guardou nada.</p>
      <p>
        No seu acompanhamento, toque no coração ao lado de uma receita ou de um conteúdo
        para ele aparecer aqui.
      </p>
      <RouterLink class="btn btn-primario" :to="{ name: 'minha-area' }">
        Ir para meu acompanhamento
      </RouterLink>
    </section>

    <template v-else>
      <section v-if="favoritos.receitas.length" class="card">
        <h3>Receitas · {{ favoritos.receitas.length }}</h3>
        <ul class="lista">
          <li v-for="r in favoritos.receitas" :key="r.receitaId">
            <div class="linha">
              <div class="texto">
                <strong>{{ r.nome }}</strong>
                <p v-if="r.descricao" class="descricao">{{ r.descricao }}</p>
                <span class="meta numerico">
                  <template v-if="r.tempoPreparo">{{ r.tempoPreparo }} min</template>
                  <template v-if="r.tempoPreparo && r.porcoes"> · </template>
                  <template v-if="r.porcoes">{{ r.porcoes }} porções</template>
                  <template v-if="r.caloriasPorPorcao">
                    · {{ Math.round(r.caloriasPorPorcao) }} kcal por porção
                  </template>
                  · guardada em {{ data(r.dataFavoritado) }}
                </span>
              </div>

              <button class="favorito marcado" title="Remover dos favoritos"
                      @click="remover('receitas', r.receitaId)">♥</button>
            </div>
          </li>
        </ul>
      </section>

      <section v-if="favoritos.conteudos.length" class="card">
        <h3>Conteúdos · {{ favoritos.conteudos.length }}</h3>
        <ul class="lista">
          <li v-for="c in favoritos.conteudos" :key="c.conteudoId">
            <div class="linha">
              <div class="texto">
                <span class="tipo">{{ c.tipo }}</span>
                <strong>{{ c.titulo }}</strong>
                <p v-if="c.corpo" class="descricao">{{ c.corpo }}</p>
                <span class="meta">
                  Por {{ c.autorNome }} · guardado em {{ data(c.dataFavoritado) }}
                </span>
              </div>

              <button class="favorito marcado" title="Remover dos favoritos"
                      @click="remover('conteudos', c.conteudoId)">♥</button>
            </div>

            <!-- RF09.2 — o recado fica atrás do player: se o vídeo não renderizar, é ele que aparece. -->
            <template v-if="linkSeguro(c.urlMidia)">
              <div v-if="idDoYouTube(c.urlMidia!)" class="moldura-video">
                <p class="recado-video">
                  O vídeo deste conteúdo não está disponível.
                  <a :href="c.urlMidia!" target="_blank" rel="noopener noreferrer">Abrir no site de origem</a>
                </p>
                <iframe
                  :src="`https://www.youtube-nocookie.com/embed/${idDoYouTube(c.urlMidia!)}`"
                  title="Vídeo do conteúdo educativo"
                  loading="lazy"
                  referrerpolicy="no-referrer"
                  allowfullscreen
                />
              </div>

              <a v-else class="midia" :href="c.urlMidia!" target="_blank" rel="noopener noreferrer">
                Abrir material em {{ dominioDe(c.urlMidia!) }} ↗
              </a>
            </template>
          </li>
        </ul>
      </section>
    </template>
  </template>
</template>

<style scoped>
.cabecalho { margin-bottom: var(--lg); }
.sub { margin: 4px 0 0; color: var(--text-secondary); font-size: 15px; }
.carregando { color: var(--text-muted); }

.card { margin-bottom: var(--md); }
.card h3 { font-size: 15px; color: var(--text-secondary); margin-bottom: var(--md); }

.lista { list-style: none; padding: 0; margin: 0; }
.lista li { padding: var(--md) 0; border-top: 1px solid var(--border); }
.lista li:first-child { border-top: none; padding-top: 0; }

.linha { display: flex; align-items: flex-start; justify-content: space-between; gap: var(--sm); }
.texto { min-width: 0; }
.linha strong { display: block; font-size: 16px; }

.tipo {
  font-size: 11px; letter-spacing: 0.05em; text-transform: uppercase;
  color: var(--text-muted); display: block; margin-bottom: 2px;
}
.descricao {
  margin: 4px 0 0; font-size: 14px; color: var(--text-secondary);
  display: -webkit-box; -webkit-line-clamp: 2; line-clamp: 2;
  -webkit-box-orient: vertical; overflow: hidden;
}
.meta { display: block; margin-top: 6px; font-size: 12px; color: var(--text-muted); }

.favorito {
  background: none; border: none; cursor: pointer; padding: 2px 4px;
  font-size: 20px; line-height: 1; color: var(--danger);
  transition: transform 0.1s;
}
.favorito:active { transform: scale(0.9); }

.midia { display: inline-block; margin-top: var(--xs); font-size: 14px; }
.moldura-video {
  position: relative; margin-top: var(--sm);
  aspect-ratio: 16 / 9; width: 100%; max-width: 480px;
  border: 1px solid var(--border); border-radius: var(--raio);
  overflow: hidden; background: var(--background);
}
.recado-video {
  position: absolute; inset: 0; margin: 0; padding: var(--md);
  display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 4px;
  text-align: center; font-size: 13px; color: var(--text-secondary);
}
.moldura-video iframe { position: relative; width: 100%; height: 100%; border: none; display: block; }

.vazio { text-align: center; padding: var(--xl) var(--lg); }
.titulo-vazio { font-size: 17px; font-weight: 500; margin: 0 0 var(--xs); }
.vazio p { color: var(--text-secondary); max-width: 420px; margin-inline: auto; }
.vazio .btn { margin-top: var(--lg); }
</style>
