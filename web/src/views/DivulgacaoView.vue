<script setup lang="ts">
/**
 * Página principal de divulgação.
 *
 * Cada afirmação daqui corresponde a algo que o sistema faz. O protótipo trazia
 * análise preditiva de hipoglicemia, sugestão de ajuste de insulina, integração
 * hospitalar, backup automático e conformidade com o CFM — nada disso existe, e
 * prometer na porta o que a banca não vai encontrar lá dentro custa caro.
 */
import LogoGlicoNutri from '../components/LogoGlicoNutri.vue'
</script>

<template>
  <section class="hero">
    <div class="limite grade-hero">
      <div>
        <span class="etiqueta">Tecnologia a serviço do cuidado</span>
        <h1>
          Nutrição clínica e controle glicêmico<br />
          <span class="destaque">no mesmo lugar.</span>
        </h1>
        <p class="chamada">
          O nutricionista prescreve com o histórico do paciente à vista. O paciente
          registra glicemia e como se sente, e enxerga o plano em vigor. Os dois lados
          olham para o mesmo dado.
        </p>

        <div class="acoes-hero">
          <RouterLink :to="{ name: 'funcionalidades' }" class="btn btn-primario">
            Conhecer as funcionalidades
          </RouterLink>
          <a href="#contato" class="btn btn-secundario">Agendar demonstração</a>
        </div>

        <p class="nota-acesso">
          O acesso é criado pela equipe — o sistema não tem autocadastro, por regra de negócio.
        </p>
      </div>

      <!-- Amostra do painel real: números de exemplo, leitura igual à do sistema. -->
      <div class="card painel-exemplo">
        <div class="linha-painel">
          <div class="avatar">MA</div>
          <div>
            <strong>Maria Almeida</strong>
            <span class="apoio">Acompanhada desde março</span>
          </div>
          <span class="selo selo-ok">No alvo</span>
        </div>

        <div class="barras">
          <span v-for="(h, i) in [38, 52, 71, 96, 64, 47, 58]" :key="i" :style="{ height: h + '%' }" />
        </div>
        <p class="legenda-barras">Glicemias dos últimos 7 dias</p>

        <div class="metricas-exemplo">
          <div>
            <span class="apoio">Média glicêmica</span>
            <strong class="numerico">104 mg/dL</strong>
          </div>
          <div>
            <span class="apoio">No alvo</span>
            <strong class="numerico verde">92%</strong>
          </div>
        </div>
      </div>
    </div>
  </section>

  <section class="limite blocos">
    <header class="cabecalho-secao">
      <h2>Dois lados do mesmo acompanhamento</h2>
      <p>
        O que o nutricionista prescreve na web é o que o paciente vê no dia a dia.
      </p>
    </header>

    <div class="grade-2">
      <article class="card">
        <h3>Para o nutricionista</h3>
        <p class="apoio">Prescrição apoiada no que o paciente registrou, não na memória da consulta.</p>
        <ul>
          <li>Necessidade energética por Harris-Benedict revisado ou Mifflin-St Jeor — a fórmula é escolha sua</li>
          <li>Plano alimentar com distribuição de macronutrientes nas faixas da SBD</li>
          <li>Banco de alimentos com a Tabela TACO, 4ª edição, e cadastro próprio</li>
          <li>Histórico glicêmico com faixa alvo definida paciente a paciente</li>
          <li>Relatórios clínicos em PDF: glicêmico, antropométrico e completo</li>
        </ul>
      </article>

      <article class="card">
        <h3>Para o paciente</h3>
        <p class="apoio">O registro leva segundos, e o plano fica a um toque.</p>
        <ul>
          <li>Glicemia registrada com o contexto da medição — jejum, pré ou pós-refeição</li>
          <li>Diário de emoções, que o sistema cruza com as glicemias do mesmo período</li>
          <li>Plano alimentar vigente, organizado por refeição</li>
          <li>Lembretes configuráveis de medição e refeição</li>
          <li>Repositório educativo, com conteúdos e receitas para favoritar</li>
        </ul>
      </article>
    </div>
  </section>

  <section class="faixa-adj">
    <div class="limite grade-adj">
      <div>
        <h2>Uma iniciativa com a ADJ de Birigui</h2>
        <p>
          A Associação de Diabetes Juvenil de Birigui é a origem do projeto e o primeiro
          serviço atendido. O sistema foi construído para o atendimento dela — e desenhado
          para caber em outras clínicas que precisem do mesmo acompanhamento.
        </p>
      </div>
      <div class="cartao-adj">
        <LogoGlicoNutri :tamanho="42" />
        <strong>ADJ · Birigui</strong>
        <span class="apoio">Associação de Diabetes Juvenil</span>
      </div>
    </div>
  </section>

  <section id="precos" class="limite precos">
    <header class="cabecalho-secao">
      <h2>Preços</h2>
      <p>
        Para a ADJ de Birigui, o uso é o do projeto: sem custo. Para outros serviços que
        queiram adotar o GlicoNutri, o modelo é definido caso a caso, conforme o número de
        nutricionistas e de pacientes acompanhados.
      </p>
    </header>

    <div class="grade-3">
      <article class="card">
        <h3>ADJ de Birigui</h3>
        <p class="valor">Projeto</p>
        <p class="apoio">Serviço de origem, com acompanhamento direto da equipe de desenvolvimento.</p>
      </article>

      <article class="card destaque-card">
        <h3>Clínica</h3>
        <p class="valor">Sob consulta</p>
        <p class="apoio">Para consultórios e clínicas de nutrição que acompanham pessoas com diabetes.</p>
      </article>

      <article class="card">
        <h3>Rede</h3>
        <p class="valor">Sob consulta</p>
        <p class="apoio">Para associações e redes com mais de uma unidade de atendimento.</p>
      </article>
    </div>
  </section>
</template>

<style scoped>
.limite { max-width: 1180px; margin-inline: auto; padding-inline: var(--lg); }

.hero { background: linear-gradient(180deg, var(--background), var(--surface)); padding: var(--xl) 0 48px; }
.grade-hero { display: grid; grid-template-columns: 1.05fr 0.95fr; gap: 48px; align-items: center; }

.etiqueta {
  display: inline-flex; align-items: center;
  background: var(--teal-light); color: var(--primary);
  font-size: 13px; font-weight: 500; padding: 5px 12px; border-radius: 999px;
  margin-bottom: var(--md);
}

.hero h1 { font-size: 44px; line-height: 52px; letter-spacing: -0.02em; }
.destaque { color: var(--primary); }
.chamada { font-size: 18px; line-height: 28px; color: var(--text-secondary); margin: var(--md) 0 var(--lg); max-width: 520px; }
.acoes-hero { display: flex; gap: var(--sm); flex-wrap: wrap; }
.acoes-hero .btn { text-decoration: none; }
.acoes-hero .btn:hover { text-decoration: none; }
.nota-acesso { font-size: 13px; color: var(--text-muted); margin-top: var(--md); }

.painel-exemplo { padding: var(--lg); }
.linha-painel { display: flex; align-items: center; gap: var(--xs); margin-bottom: var(--md); }
.linha-painel strong { display: block; font-size: 15px; }
.avatar {
  width: 38px; height: 38px; border-radius: 50%;
  background: var(--teal-light); color: var(--primary);
  display: flex; align-items: center; justify-content: center; font-weight: 600; font-size: 13px;
}
.linha-painel .selo { margin-left: auto; }

.barras { display: flex; align-items: flex-end; gap: 6px; height: 110px; }
.barras span { flex: 1; background: var(--primary); border-radius: 4px 4px 0 0; opacity: 0.85; }
.barras span:nth-child(4) { background: var(--primary-container); opacity: 1; }
.legenda-barras { font-size: 12px; color: var(--text-muted); margin: var(--xs) 0 var(--md); }

.metricas-exemplo { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sm); }
.metricas-exemplo > div { border: 1px solid var(--border); border-radius: var(--raio); padding: var(--sm); }
.metricas-exemplo strong { display: block; font-size: 20px; margin-top: 2px; }
.verde { color: var(--success); }

.blocos { padding: 48px 0; }
.cabecalho-secao { text-align: center; max-width: 640px; margin: 0 auto var(--xl); }
.cabecalho-secao p { color: var(--text-secondary); margin-top: var(--xs); }

.grade-2 { display: grid; grid-template-columns: 1fr 1fr; gap: var(--lg); }
.grade-3 { display: grid; grid-template-columns: repeat(3, 1fr); gap: var(--lg); }
.card { padding: var(--lg); }
.card ul { margin: var(--md) 0 0; padding-left: 18px; color: var(--text-secondary); }
.card li { margin-bottom: var(--xs); font-size: 15px; }
.apoio { color: var(--text-secondary); font-size: 14px; }

.faixa-adj { background: var(--background); border-block: 1px solid var(--border); padding: 48px 0; }
.grade-adj { display: grid; grid-template-columns: 1.4fr 0.6fr; gap: var(--xl); align-items: center; }
.grade-adj p { color: var(--text-secondary); margin-top: var(--xs); }
.cartao-adj {
  background: var(--surface); border: 1px solid var(--border); border-radius: var(--raio-card);
  padding: var(--lg); display: flex; flex-direction: column; align-items: center; gap: 4px; text-align: center;
}

.precos { padding: 48px 0 64px; }
.valor { font-size: 22px; font-weight: 600; color: var(--primary); margin: var(--xs) 0; }
.destaque-card { border-color: var(--primary); }

@media (max-width: 900px) {
  .grade-hero, .grade-2, .grade-3, .grade-adj { grid-template-columns: 1fr; }
  .hero h1 { font-size: 34px; line-height: 42px; }
}
</style>
