import puppeteer from 'puppeteer-core'
import { fileURLToPath } from 'node:url'
import { dirname, resolve } from 'node:path'

const AQUI = dirname(fileURLToPath(import.meta.url))
const navegador = await puppeteer.launch({
  executablePath: '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
})
const pagina = await navegador.newPage()
await pagina.goto(`file://${resolve(AQUI, 'retorno.html')}`, { waitUntil: 'networkidle0' })
await pagina.pdf({
  path: resolve(AQUI, '..', 'GlicoNutri-Retorno-Resposta-Desenvolvimento.pdf'),
  format: 'A4',
  printBackground: true,
  displayHeaderFooter: true,
  headerTemplate: '<span></span>',
  footerTemplate: '<div style="font-size:8px;width:100%;text-align:center;color:#7b8794">GlicoNutri · Retorno à resposta de desenvolvimento · <span class="pageNumber"></span>/<span class="totalPages"></span></div>',
})
await navegador.close()
console.log('PDF gerado')
