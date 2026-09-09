import puppeteer from 'puppeteer-core'
import { setTimeout as espera } from 'node:timers/promises'

const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'
const BASE = 'http://localhost:5173'
const LARGURA = 1440
const ALTURA = 900

const navegador = await puppeteer.launch({
  executablePath: CHROME,
  headless: 'shell',
  defaultViewport: { width: LARGURA, height: ALTURA, deviceScaleFactor: 2 },
  args: ['--no-sandbox', '--font-render-hinting=none', '--force-color-profile=srgb'],
})

const pagina = await navegador.newPage()
pagina.on('console', (m) => { if (m.type() === 'error') console.error('  [console]', m.text().slice(0, 120)) })

async function entrar(email, senha) {
  await pagina.goto(`${BASE}/login`, { waitUntil: 'networkidle0' })
  await pagina.type('#email', email)
  await pagina.type('#senha', senha)
  await Promise.all([
    pagina.waitForNavigation({ waitUntil: 'networkidle0' }).catch(() => {}),
    pagina.click('button[type=submit]'),
  ])
  await espera(1200)
}

async function sair() {
  await pagina.evaluate(() => localStorage.clear())
}

/** Espera a tela assentar: sem requisição pendente e sem "Carregando…". */
async function assentar(ms = 900) {
  await espera(ms)
  await pagina
    .waitForFunction(() => !document.body.innerText.includes('Carregando…'), { timeout: 6000 })
    .catch(() => {})
  await espera(400)
}

/** Telas de formulário centrado ficam perdidas num viewport largo. */
async function capturarEstreito(nome, rota, largura = 760) {
  await pagina.setViewport({ width: largura, height: 720, deviceScaleFactor: 2 })
  await pagina.goto(`${BASE}${rota}`, { waitUntil: 'networkidle0' })
  await assentar()
  const alturaReal = await pagina.evaluate(() =>
    Math.max(document.body.scrollHeight, document.documentElement.scrollHeight))
  await pagina.setViewport({ width: largura, height: alturaReal, deviceScaleFactor: 2 })
  await espera(400)
  await pagina.screenshot({ path: `telas/${nome}.png` })
  await pagina.setViewport({ width: LARGURA, height: ALTURA, deviceScaleFactor: 2 })
  console.log(`  ✓ ${nome}`)
}

async function capturar(nome, rota, preparar) {
  if (rota) {
    await pagina.goto(`${BASE}${rota}`, { waitUntil: 'networkidle0' })
    await assentar()
  }
  if (preparar) { await preparar(); await assentar(600) }

  const alturaReal = await pagina.evaluate(() =>
    Math.max(document.body.scrollHeight, document.documentElement.scrollHeight))

  await pagina.setViewport({ width: LARGURA, height: Math.min(alturaReal, 2600), deviceScaleFactor: 2 })
  await espera(500)
  await pagina.screenshot({ path: `telas/${nome}.png` })
  await pagina.setViewport({ width: LARGURA, height: ALTURA, deviceScaleFactor: 2 })
  console.log(`  ✓ ${nome}`)
}

/** Clica no primeiro elemento cujo texto casa. */
async function clicarTexto(seletor, texto) {
  const alvo = await pagina.evaluateHandle((s, t) => {
    const els = [...document.querySelectorAll(s)]
    return els.find((e) => e.textContent.trim().toLowerCase().includes(t.toLowerCase())) ?? null
  }, seletor, texto)
  const el = alvo.asElement()
  if (el) { await el.click(); return true }
  return false
}

console.log('── Telas públicas ──')
await capturarEstreito('01-login', '/login')
await capturarEstreito('02-recuperar-senha', '/recuperar-senha')

console.log('── Administrador ──')
await entrar('admin@gliconutri.local', 'GlicoNutri@2026')
await capturar('03-dashboard', '/dashboard')
await capturar('04-pacientes', '/pacientes')
await capturar('05-paciente-novo', '/pacientes/novo')
await capturar('06-alimentos', '/alimentos')
await capturar('07-alimentos-importacao', '/alimentos', async () => {
  await pagina.evaluate(() => window.scrollTo(0, 0))
})
await capturar('08-repositorio', '/repositorio')
await capturar('09-nutricionistas', '/nutricionistas')
await capturar('10-nutricionista-novo', '/nutricionistas/novo')

console.log('── Detalhe do paciente ──')
await capturar('11-paciente-acompanhamento', '/pacientes/3')
await capturar('12-paciente-plano', null, () => clicarTexto('.aba', 'Plano alimentar'))
await capturar('13-paciente-alertas', null, () => clicarTexto('.aba', 'Alertas'))
await capturar('14-paciente-cadastro', null, () => clicarTexto('.aba', 'Cadastro'))

console.log('── Paciente ──')
await sair()
await entrar('maria@paciente.br', 'Maria@2026')
await capturar('15-minha-area', '/minha-area')
await capturar('16-registro-emocional', null, () => clicarTexto('button', 'Como me sinto'))

await navegador.close()
console.log('\nconcluído')
