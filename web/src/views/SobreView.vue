<script setup lang="ts">
/**
 * Página institucional.
 *
 * O protótipo prometia integração hospitalar, interoperabilidade com padrões de
 * saúde, sincronização em tempo real e centenas de pacientes atendidos. Nada
 * disso corresponde ao sistema nem ao estágio do projeto, e é o tipo de frase
 * que a banca cobra. O texto aqui fala do que o projeto é: um trabalho de
 * conclusão de curso construído com a ADJ de Birigui, preparado para crescer.
 */
</script>

<template>
  <section class="capa">
    <div class="limite grade-capa">
      <div>
        <p class="tarja">Propósito</p>
        <h1>Cuidar do diabetes exige constância — e a constância precisa de ferramenta.</h1>
        <p class="chamada">
          O GlicoNutri nasceu para tirar o acompanhamento nutricional do papel e da memória.
          Quem convive com diabetes mede, anota e esquece; quem prescreve precisa desse
          registro na hora da consulta. O sistema liga as duas pontas.
        </p>
        <a href="#contato" class="btn btn-primario">Entrar em contato</a>
      </div>

      <div class="cartao-numeros">
        <div>
          <strong class="numerico">597</strong>
          <span>alimentos da Tabela TACO, 4ª edição</span>
        </div>
        <div>
          <strong class="numerico">2</strong>
          <span>fórmulas de gasto energético, à escolha do profissional</span>
        </div>
        <div>
          <strong class="numerico">3</strong>
          <span>relatórios clínicos em PDF</span>
        </div>
      </div>
    </div>
  </section>

  <section class="limite bloco">
    <div class="grade-2">
      <div>
        <p class="tarja">A parceria</p>
        <h2>ADJ — Associação de Diabetes Juvenil de Birigui</h2>
        <p>
          A ADJ acompanha pessoas com diabetes e suas famílias em Birigui. É de lá que veio o
          problema que o sistema resolve, e é lá que ele será usado primeiro: o atendimento
          nutricional da associação é o caso de uso que guiou cada regra implementada.
        </p>
        <p>
          A associação é a origem do projeto, não o seu limite. O sistema foi desenhado para
          atender outros serviços que precisem do mesmo acompanhamento, sem reescrita.
        </p>
      </div>

      <div>
        <p class="tarja">O trabalho</p>
        <h2>Um TCC levado a sério</h2>
        <p>
          O GlicoNutri é o trabalho de conclusão de curso de Mateus Rossini Marques Pêgo, no
          UniSalesiano de Araçatuba, sob orientação do professor Francisco Antônio de Sousa.
        </p>
        <p>
          A documentação veio antes do código: requisitos, regras de negócio, diagrama
          entidade-relacionamento, diagrama de classes, casos de uso e modelo C4. O que está
          implementado responde a um requisito escrito, e as divergências encontradas pelo
          caminho foram registradas em vez de silenciadas.
        </p>
      </div>
    </div>
  </section>

  <section class="valores">
    <div class="limite">
      <header class="cabecalho-secao">
        <h2>O que orienta as decisões</h2>
        <p>Três critérios que aparecem em cada escolha técnica do projeto.</p>
      </header>

      <div class="grade-3">
        <article class="card">
          <h3>Dado clínico não se perde</h3>
          <p>
            Nada é apagado de verdade. Paciente desativado, alimento inativado, registro
            removido — o histórico que sustenta uma prescrição antiga continua lá.
          </p>
        </article>

        <article class="card">
          <h3>A regra mora no banco também</h3>
          <p>
            As regras que corrompem dado clínico se violadas não ficam só no serviço: viram
            constraint. Foi assim que defeitos reais apareceram antes de chegar ao usuário.
          </p>
        </article>

        <article class="card">
          <h3>A conduta é do profissional</h3>
          <p>
            O sistema calcula, sugere e avisa quando algo sai da diretriz — mas quem decide a
            fórmula, a distribuição e a faixa alvo de cada paciente é o nutricionista.
          </p>
        </article>
      </div>
    </div>
  </section>

  <section class="limite bloco">
    <p class="tarja">Como o sistema é feito</p>
    <h2>Arquitetura</h2>
    <div class="grade-4">
      <div class="card"><strong>API</strong><span>C# · ASP.NET em .NET 10</span></div>
      <div class="card"><strong>Web</strong><span>Vue 3 com TypeScript</span></div>
      <div class="card"><strong>Banco</strong><span>PostgreSQL</span></div>
      <div class="card"><strong>Aplicativo</strong><span>Flutter, em desenvolvimento</span></div>
    </div>
    <p class="rodape-nota">
      A API é dona da autenticação e do controle de acesso, e é a mesma que o ambiente web e
      o aplicativo consomem.
    </p>
  </section>
</template>

<style scoped>
.limite { max-width: 1180px; margin-inline: auto; padding-inline: var(--lg); }

.capa { background: var(--background); border-bottom: 1px solid var(--border); padding: 56px 0; }
.grade-capa { display: grid; grid-template-columns: 1.3fr 0.7fr; gap: 48px; align-items: center; }
.capa h1 { font-size: 36px; line-height: 46px; }
.chamada { color: var(--text-secondary); font-size: 17px; margin: var(--md) 0 var(--lg); }
.tarja {
  font-size: 12px; letter-spacing: 0.05em; text-transform: uppercase;
  color: var(--primary); font-weight: 500; margin: 0 0 var(--xs);
}

.cartao-numeros {
  background: var(--surface); border: 1px solid var(--border); border-radius: var(--raio-card); padding: var(--lg);
}
.cartao-numeros > div { padding: var(--sm) 0; }
.cartao-numeros > div + div { border-top: 1px solid var(--border); }
.cartao-numeros strong { display: block; font-size: 28px; color: var(--primary); }
.cartao-numeros span { font-size: 14px; color: var(--text-secondary); }

.bloco { padding: 56px 0; }
.bloco p { color: var(--text-secondary); margin: var(--xs) 0 var(--sm); }
.grade-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 48px; }
.grade-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--lg); }
.grade-4 { display: grid; grid-template-columns: repeat(4, 1fr); gap: var(--md); }

.valores { background: var(--background); border-block: 1px solid var(--border); padding: 56px 0; }
.cabecalho-secao { text-align: center; max-width: 620px; margin: 0 auto var(--xl); }
.cabecalho-secao p { color: var(--text-secondary); margin-top: var(--xs); }
.card { padding: var(--lg); }
.card p { color: var(--text-secondary); font-size: 15px; margin-top: var(--xs); }

.grade-4 .card { display: flex; flex-direction: column; gap: 2px; padding: var(--md); }
.grade-4 span { font-size: 14px; color: var(--text-secondary); }
.rodape-nota { font-size: 14px; color: var(--text-muted); margin-top: var(--md); }

@media (max-width: 900px) {
  .grade-capa, .grade-2, .grade-3 { grid-template-columns: 1fr; }
  .grade-4 { grid-template-columns: 1fr 1fr; }
  .capa h1 { font-size: 28px; line-height: 36px; }
}
</style>
