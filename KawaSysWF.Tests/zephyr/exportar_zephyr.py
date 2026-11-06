"""Genera los casos de prueba para importar en Zephyr (Jira) a partir de los .feature.

Los .feature son la única fuente: si cambia un escenario, se vuelve a correr este script.
  - zephyr_scale_casos.csv : un caso por escenario con su script BDD (Gherkin), para Zephyr Scale.
  - zephyr_squad_casos.csv : un caso por escenario con pasos / datos / resultado esperado, para Zephyr Squad.

Uso:  python KawaSysWF.Tests/zephyr/exportar_zephyr.py
"""
import csv
import re
from pathlib import Path

AQUI = Path(__file__).parent
FEATURES = AQUI.parent / "Features"
CARPETA_RAIZ = "/Facturación y Cobranzas"

PALABRAS_PASO = ("Dado ", "Dada ", "Dados ", "Dadas ", "Cuando ", "Entonces ", "Y ", "Pero ")


def leer_feature(ruta):
    """Devuelve (nombre, descripción, tags, antecedentes, escenarios) de un .feature en español."""
    nombre, descripcion, tags_feature, antecedentes, escenarios = "", [], [], [], []
    tags_pendientes, actual, destino = [], None, None
    for linea in ruta.read_text(encoding="utf-8").splitlines():
        t = linea.strip()
        if not t or t.startswith("#"):
            continue
        if t.startswith("@"):
            tags_pendientes += [x[1:] for x in t.split()]
        elif t.startswith("Característica:"):
            nombre, tags_feature, tags_pendientes = t.split(":", 1)[1].strip(), tags_pendientes, []
            destino = descripcion
        elif t.startswith("Antecedentes:"):
            destino = antecedentes
        elif t.startswith(("Escenario:", "Esquema del escenario:")):
            actual = {"nombre": t.split(":", 1)[1].strip(), "tags": tags_pendientes, "pasos": [],
                      "esquema": t.startswith("Esquema")}
            tags_pendientes = []
            escenarios.append(actual)
            destino = actual["pasos"]
        elif t.startswith("Ejemplos:"):
            destino.append(t)
        elif destino is not None:
            destino.append(t)
    return nombre, " ".join(descripcion), tags_feature, antecedentes, escenarios


def separar(tags):
    jira = sorted({t for t in tags if re.fullmatch(r"KAWA-\d+", t)}, key=lambda k: int(k.split("-")[1]))
    etiquetas = [t for t in tags if t not in jira]
    return jira, etiquetas


def pasos_squad(pasos):
    """Agrupa los pasos: cada Dado/Cuando es una acción y los Entonces/Y siguientes son su resultado esperado."""
    filas, accion, datos, esperado, en_entonces = [], None, [], [], False
    for p in pasos:
        if p.startswith("Ejemplos:") or p.startswith("|"):
            datos.append(p)
            continue
        es_resultado = p.startswith("Entonces ") or (en_entonces and p.startswith(("Y ", "Pero ")))
        if es_resultado:
            en_entonces = True
            esperado.append(p)
            continue
        if accion and (esperado or not p.startswith(("Y ", "Pero "))):
            filas.append((accion, datos, esperado))
            accion, datos, esperado = None, [], []
        en_entonces = False
        accion = p if accion is None else accion + "\n" + p
    if accion or esperado:
        filas.append((accion or "", datos, esperado))
    return filas


def main():
    scale, squad = [], []
    for ruta in sorted(FEATURES.glob("*.feature")):
        feature, objetivo, tags_feature, antecedentes, escenarios = leer_feature(ruta)
        for esc in escenarios:
            jira, etiquetas = separar(tags_feature + esc["tags"])
            prioridad = "High" if "smoke" in etiquetas else "Normal"
            precondicion = "\n".join(antecedentes)
            script = "\n".join(esc["pasos"])
            scale.append({
                "Name": esc["nombre"],
                "Objective": objetivo,
                "Precondition": precondicion,
                "Folder": f"{CARPETA_RAIZ}/{feature}",
                "Status": "Approved",
                "Priority": prioridad,
                "Labels": ",".join(etiquetas),
                "Coverage (Issues)": ",".join(jira),
                "Test Script (BDD)": script,
                "Feature file": ruta.name,
            })
            for i, (accion, datos, esperado) in enumerate(pasos_squad(esc["pasos"])):
                squad.append({
                    "Summary": esc["nombre"] if i == 0 else "",
                    "Description": f"{feature}. {objetivo}\nPrecondición:\n{precondicion}" if i == 0 else "",
                    "Priority": prioridad if i == 0 else "",
                    "Labels": " ".join(etiquetas) if i == 0 else "",
                    "Requirement": ",".join(jira) if i == 0 else "",
                    "Step": accion,
                    "Test Data": "\n".join(datos),
                    "Expected Result": "\n".join(esperado),
                })

    for nombre, filas in (("zephyr_scale_casos.csv", scale), ("zephyr_squad_casos.csv", squad)):
        with open(AQUI / nombre, "w", encoding="utf-8-sig", newline="") as f:
            w = csv.DictWriter(f, fieldnames=list(filas[0].keys()), quoting=csv.QUOTE_ALL)
            w.writeheader()
            w.writerows(filas)
    print(f"{len(scale)} casos de prueba exportados a {AQUI}")


if __name__ == "__main__":
    main()
