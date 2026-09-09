from pypdf import PdfWriter, PdfReader
import pathlib

RAIZ = pathlib.Path(__file__).parent
saida = PdfWriter()

for arquivo in ("capa.pdf", "miolo.pdf"):
    for pagina in PdfReader(RAIZ / arquivo).pages:
        saida.add_page(pagina)

saida.add_metadata({
    "/Title": "GlicoNutri — Funcionalidades Implementadas",
    "/Author": "Mateus Rossini Marques Pêgo",
    "/Subject": "Documentação do sistema web e da API do GlicoNutri",
    "/Keywords": "GlicoNutri, diabetes, nutrição, ADJ Birigui, TCC",
})

destino = RAIZ / "GlicoNutri-Funcionalidades.pdf"
with open(destino, "wb") as f:
    saida.write(f)
print(f"  {destino.name}: {len(saida.pages)} páginas, {destino.stat().st_size/1048576:.1f} MB")
