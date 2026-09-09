# -*- coding: utf-8 -*-
"""Extrai a superfície da API a partir dos controllers."""
import re, json, pathlib

RAIZ = pathlib.Path.home() / "Desktop/Projects/GlicoNutri/backend/GlicoNutri.Api"

def limpar_doc(bloco):
    """Junta as linhas de /// <summary> num parágrafo."""
    if not bloco: return ""
    texto = re.sub(r"///", " ", bloco)
    texto = re.sub(r"</?(summary|remarks|para|c|see[^>]*)>", " ", texto)
    texto = re.sub(r"<param[^>]*>.*?</param>", " ", texto, flags=re.S)
    return re.sub(r"\s+", " ", texto).strip()

controllers = []
for arquivo in sorted(RAIZ.glob("Controllers/*.cs")):
    fonte = arquivo.read_text()

    for m in re.finditer(
        r"((?:^\s*///.*\n)*)\s*\[ApiController\]\s*\n\s*\[Route\(\"([^\"]+)\"\)\]\s*\n"
        r"((?:\s*\[[^\]]+\]\s*\n)*)\s*public class (\w+)", fonte, re.M):

        doc_classe, rota, attrs_classe, nome = m.groups()
        politica = re.search(r"Policy = Politicas\.(\w+)", attrs_classe)
        autorizado = "[Authorize" in attrs_classe

        # Corpo desta classe até a próxima
        inicio = m.end()
        prox = re.search(r"\n\[ApiController\]", fonte[inicio:])
        corpo = fonte[inicio: inicio + prox.start()] if prox else fonte[inicio:]

        acoes = []
        for a in re.finditer(
            r"((?:^[ \t]*///.*\n)*)((?:^[ \t]*\[[^\]]+\]\s*\n)+)[ \t]*public\s+(?:async\s+)?"
            r"(?:Task<)?([\w<>\[\], ?]+?)>?\s+(\w+)\(([^)]*)\)", corpo, re.M):

            doc, attrs, retorno, metodo, params = a.groups()
            verbo = re.search(r"\[Http(Get|Post|Put|Delete)(?:\(\"([^\"]*)\"\))?\]", attrs)
            if not verbo: continue

            pol_acao = re.search(r"Policy = Politicas\.(\w+)", attrs)
            corpo_param = re.search(r"(\w+Request)\s+\w+", params)
            arquivo_param = "IFormFile" in params

            acoes.append({
                "verbo": verbo.group(1).upper(),
                "sufixo": verbo.group(2) or "",
                "metodo": metodo,
                "doc": limpar_doc(doc),
                "perfil": (pol_acao or politica).group(1) if (pol_acao or politica) else
                          ("Público" if "AllowAnonymous" in attrs else
                           "Autenticado" if autorizado else "—"),
                "corpo": corpo_param.group(1) if corpo_param else ("arquivo" if arquivo_param else None),
                "produz": "application/pdf" if 'Produces("application/pdf")' in attrs else "JSON",
            })

        controllers.append({
            "nome": nome, "rota": rota, "doc": limpar_doc(doc_classe),
            "perfil": politica.group(1) if politica else ("Autenticado" if autorizado else "—"),
            "acoes": acoes,
        })

print(json.dumps(controllers, ensure_ascii=False, indent=1))
