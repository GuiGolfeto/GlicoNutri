import puppeteer from 'puppeteer-core'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'

const AQUI = dirname(fileURLToPath(import.meta.url))
const CHROME = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome'

const navegador = await puppeteer.launch({
  executablePath: CHROME,
  headless: 'shell',
  args: ['--no-sandbox', '--font-render-hinting=none'],
})

async function imprimir(arquivo, saida, comRodape) {
  const pagina = await navegador.newPage()
  await pagina.goto(`file://${resolve(AQUI, arquivo)}`, { waitUntil: 'networkidle0' })
  await pagina.evaluateHandle('document.fonts.ready')

  await pagina.pdf({
    path: resolve(AQUI, saida),
    format: 'A4',
    printBackground: true,
    displayHeaderFooter: comRodape,
    headerTemplate: '<div></div>',
    footerTemplate: comRodape ? `
      <div style="width:100%;font-family:Inter,-apple-system,sans-serif;font-size:7pt;
                  color:#94a3b8;padding:0 16mm;display:flex;justify-content:space-between;">
        <span>GlicoNutri · Documentação técnica do back-end</span>
        <span class="pageNumber"></span>
      </div>` : '<div></div>',
    // A capa sangra ate a borda; o miolo respira nas margens.
    margin: comRodape
      ? { top: '18mm', bottom: '20mm', left: '16mm', right: '16mm' }
      : { top: '0', bottom: '0', left: '0', right: '0' },
  })
  await pagina.close()
}

await imprimir('capa.html', 'capa.pdf', false)
await imprimir('miolo.html', 'miolo.pdf', true)
await navegador.close()
console.log('  capa e miolo impressos')
