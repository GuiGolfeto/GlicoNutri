import puppeteer from 'puppeteer-core'
import { resolve, dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const AQUI = dirname(fileURLToPath(import.meta.url))

const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'

const navegador = await puppeteer.launch({
  executablePath: CHROME,
  headless: 'shell',
  args: ['--no-sandbox', '--font-render-hinting=none'],
})

const pagina = await navegador.newPage()
await pagina.goto(`file://${resolve(AQUI, 'roteiro-impressao.html')}`, { waitUntil: 'networkidle0' })
await pagina.evaluateHandle('document.fonts.ready')

await pagina.pdf({
  path: resolve(AQUI, '..', 'GlicoNutri-Roteiro-de-Gravacao.pdf'),
  format: 'A4',
  printBackground: true,
  displayHeaderFooter: true,
  headerTemplate: '<div></div>',
  footerTemplate: `<div style="width:100%;font:9px Inter,sans-serif;color:#94a3b8;
      padding:0 15mm;display:flex;justify-content:space-between">
      <span>GlicoNutri · roteiro de gravação</span>
      <span class="pageNumber"></span></div>`,
  margin: { top: '16mm', bottom: '14mm', left: '15mm', right: '15mm' },
})

await navegador.close()
console.log('PDF gerado')
