<script setup lang="ts">
/**
 * Moldura das páginas de divulgação, abertas a quem ainda não entrou.
 *
 * Nenhum botão daqui leva a cadastro: o sistema não permite autocadastro (RN08
 * e RN09), e prometer "Começar agora" na porta seria mentir para o visitante.
 * O caminho de quem se interessa é o contato.
 */
import { ref } from 'vue'
import LogoGlicoNutri from '../components/LogoGlicoNutri.vue'

const menuAberto = ref(false)
</script>

<template>
  <div class="publico">
    <header class="topo">
      <div class="faixa">
        <RouterLink :to="{ name: 'divulgacao' }" class="marca">
          <LogoGlicoNutri :tamanho="28" />
          <strong>GlicoNutri</strong>
        </RouterLink>

        <button class="sanduiche" @click="menuAberto = !menuAberto" aria-label="Menu">
          <span></span><span></span><span></span>
        </button>

        <nav :class="{ aberto: menuAberto }" @click="menuAberto = false">
          <RouterLink :to="{ name: 'divulgacao' }">Início</RouterLink>
          <RouterLink :to="{ name: 'funcionalidades' }">Funcionalidades</RouterLink>
          <RouterLink :to="{ name: 'sobre' }">Sobre Nós</RouterLink>
          <a href="#contato">Contato</a>
          <RouterLink :to="{ name: 'login' }" class="entrar">Entrar</RouterLink>
          <a href="#contato" class="btn btn-primario demo">Agendar demonstração</a>
        </nav>
      </div>
    </header>

    <main>
      <slot />
    </main>

    <section id="contato" class="contato">
      <div class="limite">
        <h2>Quer conhecer o GlicoNutri?</h2>
        <p>
          O acesso é criado pela equipe: nutricionistas são cadastrados pelo administrador,
          e cada paciente pelo nutricionista que o acompanha. Fale com a gente para agendar
          uma demonstração.
        </p>
        <a class="btn btn-primario" href="mailto:gliconutrisuporte@gmail.com">
          gliconutrisuporte@gmail.com
        </a>
      </div>
    </section>

    <footer class="rodape">
      <div class="limite grade-rodape">
        <div class="coluna-marca">
          <div class="marca">
            <LogoGlicoNutri :tamanho="24" />
            <strong>GlicoNutri</strong>
          </div>
          <p>Apoio ao controle nutricional do diabetes, com dado clínico no lugar de palpite.</p>
        </div>

        <div>
          <p class="titulo-coluna">Produto</p>
          <RouterLink :to="{ name: 'funcionalidades' }">Funcionalidades</RouterLink>
          <RouterLink :to="{ name: 'divulgacao', hash: '#precos' }">Preços</RouterLink>
          <RouterLink :to="{ name: 'login' }">Entrar</RouterLink>
        </div>

        <div>
          <p class="titulo-coluna">Institucional</p>
          <RouterLink :to="{ name: 'sobre' }">Sobre o projeto</RouterLink>
          <a href="https://www.adjbirigui.com.br" target="_blank" rel="noopener">ADJ de Birigui</a>
          <a href="#contato">Contato</a>
        </div>

      </div>

      <div class="limite creditos">
        <span>© {{ new Date().getFullYear() }} GlicoNutri</span>
        <span>Projeto desenvolvido em parceria com a ADJ — Associação de Diabetes Juvenil de Birigui</span>
      </div>
    </footer>
  </div>
</template>

<style scoped>
.publico { background: var(--surface); }

.topo {
  position: sticky;
  top: 0;
  z-index: 10;
  background: rgba(255, 255, 255, 0.92);
  backdrop-filter: blur(8px);
  border-bottom: 1px solid var(--border);
}

.faixa {
  max-width: 1180px;
  margin-inline: auto;
  padding: var(--sm) var(--lg);
  display: flex;
  align-items: center;
  gap: var(--lg);
}

.marca { display: flex; align-items: center; gap: var(--xs); text-decoration: none; color: var(--text-primary); }
.marca strong { font-size: 18px; }
.marca:hover { text-decoration: none; }

nav { margin-left: auto; display: flex; align-items: center; gap: var(--lg); }
nav a { color: var(--text-secondary); font-size: 15px; }
nav a:hover { color: var(--primary); text-decoration: none; }
nav a.router-link-active { color: var(--primary); font-weight: 500; }
nav .entrar { color: var(--text-primary); }
nav .demo { color: #fff; font-size: 14px; padding: 8px 16px; }
nav .demo:hover { color: #fff; }

.sanduiche { display: none; background: none; border: none; cursor: pointer; padding: 6px; margin-left: auto; }
.sanduiche span { display: block; width: 20px; height: 2px; background: var(--text-primary); margin: 4px 0; }

.limite { max-width: 1180px; margin-inline: auto; padding-inline: var(--lg); }

.contato { background: var(--primary); color: #fff; padding: var(--xl) 0; text-align: center; }
.contato h2 { color: #fff; }
.contato p { max-width: 620px; margin: var(--sm) auto var(--lg); color: rgba(255, 255, 255, 0.85); }
.contato .btn { background: #fff; color: var(--primary); }

.rodape { border-top: 1px solid var(--border); padding: var(--xl) 0 var(--lg); background: var(--background); }
.grade-rodape { display: grid; grid-template-columns: 2fr 1fr 1fr; gap: var(--lg); }
.coluna-marca p { color: var(--text-secondary); font-size: 14px; max-width: 300px; margin: var(--xs) 0 0; }
.titulo-coluna {
  font-size: 12px; letter-spacing: 0.05em; text-transform: uppercase;
  color: var(--text-muted); margin: 0 0 var(--xs);
}
.rodape .grade-rodape a { display: block; font-size: 14px; color: var(--text-secondary); margin-bottom: 6px; }
.rodape .grade-rodape a:hover { color: var(--primary); }

.creditos {
  margin-top: var(--xl); padding-top: var(--md); border-top: 1px solid var(--border);
  display: flex; justify-content: space-between; gap: var(--md);
  font-size: 13px; color: var(--text-muted);
}

@media (max-width: 860px) {
  .sanduiche { display: block; }
  nav {
    display: none; position: absolute; top: 100%; left: 0; right: 0;
    flex-direction: column; align-items: stretch; gap: var(--xs);
    background: var(--surface); border-bottom: 1px solid var(--border); padding: var(--md);
  }
  nav.aberto { display: flex; }
  .grade-rodape { grid-template-columns: 1fr 1fr; }
  .creditos { flex-direction: column; }
}
</style>
