# -*- coding: utf-8 -*-
"""Extrai serviços, suas operações e as regras de negócio citadas no código."""
import re, json, pathlib
from collections import defaultdict

RAIZ = pathlib.Path.home() / "Desktop/Projects/GlicoNutri/backend/GlicoNutri.Api"

def limpar(bloco):
    if not bloco: return ""
    t = re.sub(r"///", " ", bloco)
    t = re.sub(r"</?(summary|remarks|para|c|see[^>]*)>", " ", t)
    return re.sub(r"\s+", " ", t).strip()

servicos = []
for arquivo in sorted(RAIZ.glob("Services/*.cs")) + sorted(RAIZ.glob("Repositories/*.cs")):
    fonte = arquivo.read_text()
    nome = arquivo.stem

    # Documento da classe concreta
    doc_classe = ""
    m = re.search(r"((?:^\s*///.*\n)+)\s*public (?:sealed )?(?:static )?class (\w+)", fonte, re.M)
    if m: doc_classe = limpar(m.group(1))

    # Operações públicas da interface, que descrevem o contrato
    operacoes = []
    inter = re.search(r"public interface (I\w+)\s*\{(.*?)\n\}", fonte, re.S)
    if inter:
        for op in re.finditer(r"((?:^\s*///.*\n)*)\s*(?:Task<)?([\w<>\[\], ?]+?)>?\s+(\w+)\(", inter.group(2), re.M):
            doc, retorno, metodo = op.groups()
            if metodo in ("get", "set"): continue
            operacoes.append({"nome": metodo, "retorno": retorno.strip(), "doc": limpar(doc)})

    # Métodos públicos de classe estática (calculadora, validador)
    if not operacoes:
        for op in re.finditer(
            r"((?:^\s*///.*\n)*)\s*public static [\w<>\[\], ?()]+ (\w+)\(", fonte, re.M):
            doc, metodo = op.groups()
            operacoes.append({"nome": metodo, "retorno": "", "doc": limpar(doc)})

    regras = sorted(set(re.findall(r"\bRN\d{2}\b", fonte)))
    requisitos = sorted(set(re.findall(r"\bRF\d{2}(?:\.\d)?\b", fonte)))
    casos = sorted(set(re.findall(r"\bUC\d{3}\b", fonte)))

    servicos.append({
        "nome": nome, "doc": doc_classe, "operacoes": operacoes,
        "regras": regras, "requisitos": requisitos, "casos": casos,
        "linhas": len(fonte.split("\n")),
    })

print(json.dumps(servicos, ensure_ascii=False, indent=1))
