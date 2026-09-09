from pypdf import PdfWriter, PdfReader
import pathlib

RAIZ = pathlib.Path(__file__).parent
saida = PdfWriter()
for arquivo in ("capa.pdf", "miolo.pdf"):
    for pagina in PdfReader(RAIZ / arquivo).pages:
        saida.add_page(pagina)

saida.add_metadata({
    "/Title": "GlicoNutri — Documentação Técnica do Back-end",
    "/Subject": "API REST, modelo de dados, regras de negócio e decisões de implementação",
    "/Keywords": "GlicoNutri, API, ASP.NET, PostgreSQL, TCC",
})

destino = RAIZ / "GlicoNutri-Backend.pdf"
with open(destino, "wb") as f:
    saida.write(f)
print(f"  {destino.name}: {len(saida.pages)} páginas, {destino.stat().st_size/1048576:.1f} MB")
