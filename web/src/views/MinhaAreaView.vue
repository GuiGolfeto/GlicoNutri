<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { api, mensagemDeErro } from '../api/client'
import type {
  Alerta, ConteudoEducativo, Favoritos, HistoricoGlicemico, PlanoAlimentar,
  Receita, Referencia, SerieGlicemica,
} from '../api/tipos'
import GraficoLinha, { type PontoGrafico } from '../components/GraficoLinha.vue'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const id = computed(() => auth.usuario?.usuarioId ?? 0)

const historico = ref<HistoricoGlicemico | null>(null)
const serie = ref<SerieGlicemica | null>(null)
const plano = ref<PlanoAlimentar | null>(null)
const alertas = ref<Alerta[]>([])
const receitas = ref<Receita[]>([])
const conteudos = ref<ConteudoEducativo[]>([])

// RF09.3 — favoritos. Guardados como conjunto de ids para o coração de cada
// item saber seu estado sem varrer a lista a cada render.
const favoritosReceita = ref<Set<number>>(new Set())
const favoritosConteudo = ref<Set<number>>(new Set())

/**
 * RF09.2 — mídia por link externo.
 *
 * Vídeo do YouTube é embutido; qualquer outro endereço vira link, com o domínio
 * à mostra para o paciente saber para onde está indo. O aviso de "não carregou"
 * fica ATRÁS do player: se o iframe não renderizar — vídeo removido, bloqueio de
 * rede, site fora do ar —, é o aviso que aparece no lugar. Detectar a falha por
 * evento não funciona: iframe e âncora disparam load mesmo em página de erro.
 */
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
const contextos = ref<Referencia[]>([])

const carregando = ref(true)

/**
 * RF09.3 — marca e desmarca favorito. A tela muda na hora e desfaz se a chamada
 * falhar: esperar o servidor para acender um coração deixa o toque sem resposta.
 */
async function alternarFavorito(tipo: 'conteudos' | 'receitas', idItem: number) {
  const conjunto = tipo === 'receitas' ? favoritosReceita : favoritosConteudo
  const marcado = conjunto.value.has(idItem)

  const novo = new Set(conjunto.value)
  if (marcado) novo.delete(idItem)
  else novo.add(idItem)
  conjunto.value = novo

  try {
    const rota = `/pacientes/${id.value}/favoritos/${tipo}/${idItem}`
    if (marcado) await api.delete(rota)
    else await api.put(rota)
  } catch {
    const desfeito = new Set(conjunto.value)
    if (marcado) desfeito.add(idItem)
    else desfeito.delete(idItem)
    conjunto.value = desfeito
  }
}

/** Só http e https saem do sistema como mídia — o back-end aplica a mesma regra. */
const linkSeguro = (url: string | null) =>
  !!url && (url.startsWith('https://') || url.startsWith('http://'))
const erro = ref('')
const sucesso = ref('')
const registrando = ref(false)
const enviando = ref(false)

const form = reactive({ valor: null as number | null, contextoId: null as number | null, observacao: '' })

// UC012 — registro emocional pelo próprio paciente.
const emocoes = ref<Referencia[]>([])
const registrandoEmocao = ref(false)
const formEmocao = reactive({
  estadosEmocionaisIds: [] as number[],
  intensidade: 3,
  descricao: '',
})

async function carregar() {
  carregando.value = true
  try {
    const [h, s, p, a, r, c, f] = await Promise.all([
      api.get<HistoricoGlicemico>(`/pacientes/${id.value}/glicemia`, { params: { dias: 7 } }),
      api.get<SerieGlicemica>(`/pacientes/${id.value}/glicemia/serie`, { params: { dias: 30 } }),
      api.get<PlanoAlimentar | ''>(`/pacientes/${id.value}/plano-alimentar`),
      api.get<Alerta[]>(`/pacientes/${id.value}/alertas`),
      api.get<Receita[]>('/receitas'),
      api.get<ConteudoEducativo[]>('/conteudos'),
      api.get<Favoritos>(`/pacientes/${id.value}/favoritos`),
    ])
    historico.value = h.data
    serie.value = s.data
    plano.value = p.data || null
    alertas.value = a.data.filter((x) => x.ativo)
    receitas.value = r.data.slice(0, 6)
    conteudos.value = c.data.slice(0, 6)
    favoritosConteudo.value = new Set(f.data.conteudos.map((x) => x.conteudoId))
    favoritosReceita.value = new Set(f.data.receitas.map((x) => x.receitaId))
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Não foi possível carregar seus dados.')
  } finally {
    carregando.value = false
  }
}

onMounted(async () => {
  const [ctx, emo] = await Promise.all([
    api.get<Referencia[]>('/referencias/contextos-glicemia'),
    api.get<Referencia[]>('/referencias/estados-emocionais'),
  ])
  contextos.value = ctx.data
  emocoes.value = emo.data
  form.contextoId = ctx.data[0]?.id ?? null

  // RN35 — ao abrir a sessão, os alertas pendentes fora da janela expiram.
  api.post(`/pacientes/${id.value}/alertas/verificar-expirados`, {}).catch(() => {})

  await carregar()
})

async function registrar() {
  erro.value = ''
  sucesso.value = ''
  enviando.value = true
  try {
    await api.post(`/pacientes/${id.value}/glicemia`, {
      valor: form.valor,
      contextoId: form.contextoId,
      observacao: form.observacao || null,
    })
    form.valor = null
    form.observacao = ''
    registrando.value = false
    sucesso.value = 'Glicemia registrada.'
    await carregar()
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}

/** RN01 do UC012 — ao menos uma emoção precisa estar selecionada. */
function alternarEmocao(id: number) {
  const i = formEmocao.estadosEmocionaisIds.indexOf(id)
  if (i >= 0) formEmocao.estadosEmocionaisIds.splice(i, 1)
  else formEmocao.estadosEmocionaisIds.push(id)
}

async function registrarEmocao() {
  erro.value = ''
  sucesso.value = ''
  enviando.value = true
  try {
    await api.post(`/pacientes/${id.value}/emocoes`, {
      estadosEmocionaisIds: formEmocao.estadosEmocionaisIds,
      intensidade: formEmocao.intensidade,
      descricao: formEmocao.descricao || null,
    })
    formEmocao.estadosEmocionaisIds = []
    formEmocao.intensidade = 3
    formEmocao.descricao = ''
    registrandoEmocao.value = false
    sucesso.value = 'Registro emocional salvo.'
  } catch (e) {
    erro.value = mensagemDeErro(e)
  } finally {
    enviando.value = false
  }
}

const ultima = computed(() => historico.value?.registros[0] ?? null)

const pontos = computed<PontoGrafico[]>(() =>
  serie.value?.pontos.map((p) => ({
    data: p.dataHora, valor: p.valor, destaque: p.foraDoAlvo, rotulo: p.contexto,
  })) ?? [])

const horaCurta = (h: string | null) => (h ? h.slice(0, 5) : null)

const quando = (iso: string) =>
  new Date(iso).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })

const primeiroNome = computed(() => auth.usuario?.nome.split(' ')[0] ?? '')
</script>

<template>
  <div class="entre cabecalho">
    <div>
      <h1>Olá, {{ primeiroNome }}</h1>
      <p class="sub">Seu acompanhamento nutricional</p>
    </div>
    <div class="linha">
      <button v-if="!registrandoEmocao" class="btn btn-secundario" @click="registrandoEmocao = true">
        Como me sinto
      </button>
      <button v-if="!registrando" class="btn btn-primario" @click="registrando = true">
        Registrar glicemia
      </button>
    </div>
  </div>

  <div v-if="erro" class="aviso aviso-erro">{{ erro }}</div>
  <div v-if="sucesso" class="aviso aviso-sucesso">{{ sucesso }}</div>

  <!-- UC004 — registro pelo próprio paciente -->
  <section v-if="registrando" class="card">
    <h3>Nova medição</h3>
    <form @submit.prevent="registrar">
      <div class="grade-2">
        <div class="campo">
          <label for="valor">Glicemia (mg/dL) *</label>
          <input id="valor" v-model.number="form.valor" type="number" min="0" max="600" required autofocus />
        </div>
        <div class="campo">
          <label for="contexto">Momento da medição *</label>
          <select id="contexto" v-model="form.contextoId" required>
            <option v-for="c in contextos" :key="c.id" :value="c.id">{{ c.descricao }}</option>
          </select>
        </div>
      </div>
      <div class="campo">
        <label for="obs">Como você está se sentindo?</label>
        <input id="obs" v-model="form.observacao" maxlength="500" placeholder="Opcional" />
      </div>
      <div class="linha">
        <button class="btn btn-primario" type="submit" :disabled="enviando || !form.valor">
          {{ enviando ? 'Salvando…' : 'Salvar' }}
        </button>
        <button class="btn btn-secundario" type="button" @click="registrando = false">Cancelar</button>
      </div>
    </form>
  </section>

  <!-- UC012 — registro emocional; opcional por natureza (RN22) -->
  <section v-if="registrandoEmocao" class="card">
    <h3>Como você está se sentindo?</h3>
    <form @submit.prevent="registrarEmocao">
      <div class="emocoes">
        <button v-for="e in emocoes" :key="e.id" type="button" class="emocao"
                :class="{ ativa: formEmocao.estadosEmocionaisIds.includes(e.id) }"
                @click="alternarEmocao(e.id)">
          {{ e.descricao }}
        </button>
      </div>

      <div class="campo intensidade">
        <label for="intensidade">Intensidade: <strong>{{ formEmocao.intensidade }}</strong> de 5</label>
        <input id="intensidade" v-model.number="formEmocao.intensidade" type="range" min="1" max="5" />
        <span class="ajuda">1 é leve, 5 é muito intenso.</span>
      </div>

      <div class="campo">
        <label for="desc-emocao">Quer contar o que aconteceu?</label>
        <input id="desc-emocao" v-model="formEmocao.descricao" maxlength="500" placeholder="Opcional" />
      </div>

      <div class="linha">
        <button class="btn btn-primario" type="submit"
                :disabled="enviando || formEmocao.estadosEmocionaisIds.length === 0">
          {{ enviando ? 'Salvando…' : 'Salvar' }}
        </button>
        <button class="btn btn-secundario" type="button" @click="registrandoEmocao = false">Cancelar</button>
      </div>
      <p class="apoio">
        Registrar como você se sente ajuda seu nutricionista a entender o que
        influencia sua glicemia. É opcional.
      </p>
    </form>
  </section>

  <p v-if="carregando" class="vazio">Carregando…</p>

  <template v-else>
    <div class="colunas">
      <section class="card">
        <h3>Última glicemia</h3>
        <template v-if="ultima">
          <div class="destaque" :class="{ fora: ultima.foraDoAlvo }">
            <strong class="numerico valor">{{ ultima.valor }}</strong>
            <span class="unidade">mg/dL</span>
            <span class="selo" :class="ultima.foraDoAlvo ? 'selo-perigo' : 'selo-ok'">
              {{ ultima.foraDoAlvo ? 'Fora do alvo' : 'No alvo' }}
            </span>
          </div>
          <p class="apoio numerico">{{ ultima.contexto }} · {{ quando(ultima.dataHora) }}</p>
          <p class="apoio numerico">
            Sua faixa alvo: {{ ultima.minAlvoAplicado }}–{{ ultima.maxAlvoAplicado }} mg/dL
          </p>
        </template>
        <p v-else class="vazio">Nenhuma medição registrada ainda.</p>
      </section>

      <section class="card">
        <h3>Últimos 7 dias</h3>
        <template v-if="historico && historico.resumo.totalRegistros">
          <div class="indicadores">
            <div><span class="rotulo">Medições</span><strong class="numerico">{{ historico.resumo.totalRegistros }}</strong></div>
            <div><span class="rotulo">Média</span><strong class="numerico">{{ historico.resumo.media }}</strong></div>
            <div>
              <span class="rotulo">No alvo</span>
              <strong class="numerico" :class="(historico.resumo.percentualNoAlvo ?? 100) < 70 ? 'ruim' : 'bom'">
                {{ historico.resumo.percentualNoAlvo }}%
              </strong>
            </div>
          </div>
        </template>
        <p v-else class="vazio">Sem medições no período.</p>
      </section>
    </div>

    <section class="card">
      <h3>Evolução</h3>
      <GraficoLinha :pontos="pontos" :min-alvo="serie?.minAlvo" :max-alvo="serie?.maxAlvo" unidade="mg/dL" />
    </section>

    <div class="colunas">
      <section class="card">
        <h3>Seu plano alimentar</h3>
        <template v-if="plano">
          <p class="objetivo">{{ plano.objetivo }}</p>
          <div v-if="plano.distribuicao" class="macros numerico">
            <span>{{ plano.totalCaloriasPrescritas }} kcal/dia</span>
            <span>CHO {{ plano.distribuicao.carboidratosGramas }} g</span>
            <span>PTN {{ plano.distribuicao.proteinasGramas }} g</span>
            <span>LIP {{ plano.distribuicao.lipidiosGramas }} g</span>
          </div>

          <ul v-if="plano.itens.length" class="refeicoes">
            <li v-for="t in plano.totaisPorRefeicao" :key="t.refeicao">
              <span class="nome">{{ t.refeicao }}</span>
              <span class="numerico">{{ t.calorias }} kcal</span>
            </li>
          </ul>
          <p v-else class="apoio">As refeições ainda não foram montadas.</p>
        </template>
        <p v-else class="vazio">Você ainda não tem plano alimentar ativo.</p>
      </section>

      <section class="card">
        <h3>Seus lembretes</h3>
        <ul v-if="alertas.length" class="lembretes">
          <li v-for="a in alertas" :key="a.id">
            <span class="numerico hora">
              {{ horaCurta(a.horario1) }}<template v-if="a.horario2"> e {{ horaCurta(a.horario2) }}</template>
            </span>
            <span class="texto">{{ a.mensagem || a.tipo }}</span>
          </li>
        </ul>
        <p v-else class="vazio">Nenhum lembrete configurado.</p>
        <p class="apoio">Os lembretes são configurados pelo seu nutricionista.</p>
      </section>
    </div>

    <section v-if="receitas.length" class="card">
      <h3>Receitas para você</h3>
      <ul class="receitas">
        <li v-for="r in receitas" :key="r.id">
          <div class="linha-item">
            <div>
              <strong>{{ r.nome }}</strong>
              <span class="apoio numerico">
                <template v-if="r.nutricional.caloriasPorPorcao">
                  {{ r.nutricional.caloriasPorPorcao }} kcal por porção
                </template>
                <template v-else>{{ r.nutricional.calorias }} kcal no total</template>
                <template v-if="r.tempoPreparo"> · {{ r.tempoPreparo }} min</template>
              </span>
            </div>

            <!-- RF09.3 -->
            <button
              class="favorito"
              :class="{ marcado: favoritosReceita.has(r.id) }"
              :aria-pressed="favoritosReceita.has(r.id)"
              :title="favoritosReceita.has(r.id) ? 'Remover dos favoritos' : 'Guardar nos favoritos'"
              @click="alternarFavorito('receitas', r.id)"
            >♥</button>
          </div>
        </li>
      </ul>
    </section>

    <section v-if="conteudos.length" class="card">
      <h3>Para ler</h3>
      <ul class="conteudos">
        <li v-for="c in conteudos" :key="c.id">
          <div class="linha-item">
            <div>
              <span class="tipo">{{ c.tipo }}</span>
              <strong>{{ c.titulo }}</strong>
            </div>
            <button
              class="favorito"
              :class="{ marcado: favoritosConteudo.has(c.id) }"
              :aria-pressed="favoritosConteudo.has(c.id)"
              :title="favoritosConteudo.has(c.id) ? 'Remover dos favoritos' : 'Guardar nos favoritos'"
              @click="alternarFavorito('conteudos', c.id)"
            >♥</button>
          </div>

          <p v-if="c.corpo" class="corpo">{{ c.corpo }}</p>

          <!-- RF09.2 — a mídia é link externo, e link externo sai do ar sem avisar. -->
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

<style scoped>
.cabecalho { margin-bottom: var(--lg); }
.sub { margin: 4px 0 0; color: var(--text-secondary); font-size: 15px; }
.card { margin-bottom: var(--md); }
.card h3 { font-size: 15px; color: var(--text-secondary); margin-bottom: var(--md); }
.apoio { font-size: 13px; color: var(--text-muted); margin: var(--xs) 0 0; }

.colunas { display: grid; grid-template-columns: 1fr 1fr; gap: var(--md); align-items: start; }
@media (max-width: 900px) { .colunas { grid-template-columns: 1fr; } }

.destaque { display: flex; align-items: baseline; gap: var(--xs); }
.valor { font-size: 42px; line-height: 1; color: var(--primary); }
.destaque.fora .valor { color: var(--danger); }
.unidade { font-size: 14px; color: var(--text-secondary); }
.destaque .selo { margin-left: var(--xs); }

.receitas, .conteudos { list-style: none; padding: 0; margin: 0; }
.receitas li, .conteudos li { padding: var(--sm) 0; border-top: 1px solid var(--border); }
.receitas li:first-child, .conteudos li:first-child { border-top: none; padding-top: 0; }

.linha-item { display: flex; align-items: flex-start; justify-content: space-between; gap: var(--sm); }
.linha-item strong { display: block; font-size: 15px; }

.tipo {
  font-size: 11px; letter-spacing: 0.05em; text-transform: uppercase;
  color: var(--text-muted); display: block; margin-bottom: 2px;
}
.corpo {
  margin: var(--xs) 0 0; font-size: 14px; color: var(--text-secondary);
  display: -webkit-box; -webkit-line-clamp: 2; line-clamp: 2;
  -webkit-box-orient: vertical; overflow: hidden;
}
.midia { display: inline-block; margin-top: var(--xs); font-size: 14px; }

/* O recado fica no fundo da moldura; o iframe cobre por cima quando carrega. */
.moldura-video {
  position: relative; margin-top: var(--xs);
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

.favorito {
  background: none; border: none; cursor: pointer; padding: 2px 4px;
  font-size: 20px; line-height: 1; color: var(--border);
  transition: color 0.15s, transform 0.1s;
}
.favorito:hover { color: var(--danger); }
.favorito:active { transform: scale(0.9); }
.favorito.marcado { color: var(--danger); }

.indicadores { display: flex; gap: var(--lg); }
.indicadores > div { display: flex; flex-direction: column; }
.rotulo { font-size: 11px; letter-spacing: 0.05em; text-transform: uppercase; color: var(--text-muted); }
.indicadores strong { font-size: 24px; }
.bom { color: var(--primary); }
.ruim { color: var(--danger); }

.objetivo { font-size: 16px; margin: 0 0 var(--xs); }
.macros { display: flex; gap: var(--md); flex-wrap: wrap; font-size: 14px; color: var(--text-secondary); }

.refeicoes, .lembretes, .receitas { list-style: none; margin: var(--md) 0 0; padding: 0; }
.refeicoes li, .lembretes li, .receitas li {
  display: flex; align-items: center; gap: var(--sm);
  padding: 7px 0; border-bottom: 1px solid var(--border); font-size: 15px;
}
.refeicoes li:last-child, .lembretes li:last-child, .receitas li:last-child { border-bottom: none; }
.nome, .texto { flex: 1; }
.hora { color: var(--primary); font-weight: 500; min-width: 92px; }

.emocoes { display: flex; flex-wrap: wrap; gap: var(--xs); margin-bottom: var(--md); }
.emocao {
  font-family: inherit; font-size: 15px; padding: 9px 18px;
  border: 1px solid var(--border); border-radius: 9999px;
  background: var(--surface); color: var(--text-secondary); cursor: pointer;
  transition: background 0.15s, border-color 0.15s;
}
.emocao:hover { background: var(--hover); }
.emocao.ativa { background: var(--teal-light); border-color: var(--primary); color: var(--primary); font-weight: 500; }

.intensidade input[type=range] { accent-color: var(--primary); }
.receitas li { flex-direction: column; align-items: flex-start; gap: 2px; }
</style>
