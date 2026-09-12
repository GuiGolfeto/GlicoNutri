<script setup lang="ts">
/**
 * Página de funcionalidades.
 *
 * A regra aqui é a mesma da principal: cada item corresponde a tela ou endpoint
 * que existe. Saíram do protótipo a análise preditiva, o insight de ajuste de
 * insulina, a contagem automática por índice e carga glicêmica — o índice
 * glicêmico ainda nem está populado —, o backup automático, a conformidade com
 * o CFM e os botões das lojas, com o aplicativo ainda não publicado.
 */
</script>

<template>
  <section class="capa">
    <div class="limite">
      <span class="etiqueta">O que o sistema faz</span>
      <h1>Do registro do paciente à prescrição assinada.</h1>
      <p class="chamada">
        Dois ambientes, um banco de dados. O que o paciente registra alimenta o que o
        nutricionista decide.
      </p>
    </div>
  </section>

  <section class="limite modulo">
    <p class="tarja">Ambiente web · nutricionista</p>
    <h2>O consultório em uma tela</h2>

    <div class="grade-2">
      <article class="card">
        <h3>Acompanhamento glicêmico</h3>
        <p>
          Cada medição entra com o contexto — jejum, pré ou pós-refeição — e é classificada
          contra a faixa alvo daquele paciente, definida pelo nutricionista. Mudou a faixa,
          o histórico inteiro é reclassificado, sem sobrar leitura antiga fora do critério.
        </p>
      </article>

      <article class="card">
        <h3>Plano alimentar</h3>
        <p>
          Distribuição de macronutrientes nas faixas da Diretriz da Sociedade Brasileira de
          Diabetes, com o padrão no centro de cada uma e ajuste livre pelo profissional. Os
          itens vêm da Tabela TACO, 4ª edição, com 597 alimentos, e os totais são calculados
          por refeição.
        </p>
      </article>

      <article class="card">
        <h3>Necessidade energética</h3>
        <p>
          Harris-Benedict na revisão de Roza e Shizgal ou Mifflin-St Jeor: a escolha é do
          nutricionista, em cada cálculo. A fórmula usada fica registrada junto do resultado,
          então o histórico diz por qual critério cada valor saiu.
        </p>
      </article>

      <article class="card">
        <h3>Emoção e glicemia lado a lado</h3>
        <p>
          O paciente registra como se sente; o sistema cruza esse registro com as glicemias do
          mesmo período. Serve para a conversa da consulta, não para diagnóstico.
        </p>
      </article>

      <article class="card">
        <h3>Alertas</h3>
        <p>
          Lembretes de medição, medicação e refeição, configurados por paciente. O disparo
          nasce pendente, é concluído quando o paciente registra e expira sozinho depois de
          duas horas sem resposta.
        </p>
      </article>

      <article class="card">
        <h3>Relatórios em PDF</h3>
        <p>
          Glicêmico, antropométrico e completo, com o período escolhido. É o documento que
          sai do sistema e vai para a mão do paciente ou para o prontuário.
        </p>
      </article>
    </div>
  </section>

  <section class="faixa-app">
    <div class="limite grade-app">
      <div class="moldura-celular">
        <div class="tela-celular">
          <div class="topo-celular">
            <span class="apoio-claro">Bom dia,</span>
            <strong>Olá, Ricardo</strong>
          </div>
          <div class="cartao-glicemia">
            <span class="apoio">Glicemia atual</span>
            <strong class="numerico">112 mg/dL</strong>
            <span class="selo selo-ok">Dentro do alvo</span>
          </div>
          <div class="atalhos">
            <span>Diário</span>
            <span>Plano</span>
          </div>
          <div class="mini-barras">
            <span v-for="(h, i) in [45, 70, 55, 88, 62]" :key="i" :style="{ height: h + '%' }" />
          </div>
        </div>
      </div>

      <div>
        <p class="tarja">Área do paciente</p>
        <h2>O acompanhamento na mão de quem convive com a doença</h2>
        <p class="chamada-clara">
          Disponível na web e, em breve, também em aplicativo. Os dois consomem a mesma API,
          então o registro feito em um aparece no outro.
        </p>

        <ul class="lista-clara">
          <li><strong>Registro de glicemia e emoção</strong> — com contexto e observação</li>
          <li><strong>Plano alimentar detalhado</strong> — por refeição, com quantidades e calorias</li>
          <li><strong>Conteúdo educativo</strong> — artigos, dicas e receitas publicados pela equipe</li>
          <li><strong>Favoritos</strong> — o que o paciente quiser guardar fica à mão</li>
        </ul>
      </div>
    </div>
  </section>

  <section class="limite modulo">
    <p class="tarja">Dados e acesso</p>
    <h2>O cuidado com o dado clínico</h2>

    <div class="grade-3">
      <article class="card">
        <h3>Acesso por perfil</h3>
        <p>
          Nutricionista, paciente e administrador enxergam coisas diferentes, e cada
          nutricionista alcança apenas os pacientes vinculados a ele.
        </p>
      </article>

      <article class="card">
        <h3>Senha sob regra</h3>
        <p>
          Credenciais em bcrypt, senha provisória obrigatória no primeiro acesso e bloqueio
          progressivo depois de cinco tentativas inválidas.
        </p>
      </article>

      <article class="card">
        <h3>Nada se apaga</h3>
        <p>
          Remoção é sempre lógica: o histórico clínico é preservado, inclusive o de pacientes
          e alimentos desativados.
        </p>
      </article>
    </div>
  </section>
</template>

<style scoped>
.limite { max-width: 1180px; margin-inline: auto; padding-inline: var(--lg); }

.capa { background: var(--background); border-bottom: 1px solid var(--border); padding: 56px 0; text-align: center; }
.etiqueta {
  display: inline-flex; background: var(--teal-light); color: var(--primary);
  font-size: 13px; font-weight: 500; padding: 5px 12px; border-radius: 999px; margin-bottom: var(--md);
}
.capa h1 { font-size: 40px; line-height: 48px; }
.chamada { font-size: 18px; color: var(--text-secondary); max-width: 620px; margin: var(--md) auto 0; }

.modulo { padding: 56px 0; }
.tarja {
  font-size: 12px; letter-spacing: 0.05em; text-transform: uppercase;
  color: var(--primary); font-weight: 500; margin: 0 0 var(--xs);
}
.modulo h2 { margin-bottom: var(--lg); }

.grade-2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--lg); }
.grade-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--lg); }
.card { padding: var(--lg); }
.card p { color: var(--text-secondary); font-size: 15px; margin: var(--xs) 0 0; }

.faixa-app { background: var(--primary); color: #fff; padding: 56px 0; }
.grade-app { display: grid; grid-template-columns: 0.8fr 1.2fr; gap: 48px; align-items: center; }
.faixa-app h2 { color: #fff; }
.faixa-app .tarja { color: rgba(255, 255, 255, 0.75); }
.chamada-clara { color: rgba(255, 255, 255, 0.85); margin: var(--sm) 0 var(--lg); }
.lista-clara { list-style: none; padding: 0; margin: 0; }
.lista-clara li {
  padding: var(--sm) 0; border-top: 1px solid rgba(255, 255, 255, 0.18);
  color: rgba(255, 255, 255, 0.85); font-size: 15px;
}
.lista-clara strong { color: #fff; }

.moldura-celular {
  background: #0f172a; border-radius: 28px; padding: 10px; max-width: 260px;
  margin-inline: auto; box-shadow: 0 18px 40px rgba(15, 23, 42, 0.35);
}
.tela-celular { background: var(--surface); border-radius: 20px; padding: var(--md); color: var(--text-primary); }
.topo-celular { margin-bottom: var(--md); }
.apoio-claro { font-size: 12px; color: var(--text-muted); display: block; }
.cartao-glicemia {
  background: var(--teal-light); border-radius: var(--raio); padding: var(--sm); margin-bottom: var(--sm);
}
.cartao-glicemia strong { display: block; font-size: 22px; color: var(--primary); margin: 2px 0 6px; }
.apoio { font-size: 12px; color: var(--text-secondary); }
.atalhos { display: grid; grid-template-columns: 1fr 1fr; gap: var(--xs); margin-bottom: var(--sm); }
.atalhos span {
  border: 1px solid var(--border); border-radius: var(--raio); padding: var(--xs);
  text-align: center; font-size: 13px; color: var(--text-secondary);
}
.mini-barras { display: flex; align-items: flex-end; gap: 4px; height: 56px; }
.mini-barras span { flex: 1; background: var(--primary); opacity: 0.8; border-radius: 3px 3px 0 0; }

@media (max-width: 900px) {
  .grade-2, .grade-3, .grade-app { grid-template-columns: 1fr; }
  .capa h1 { font-size: 30px; line-height: 38px; }
}
</style>
