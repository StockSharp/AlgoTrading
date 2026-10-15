# HFT-Spreader für FORTS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert das Verhalten eines HFT-Spreaders auf dem FORTS-Markt. Sie überwacht kontinuierlich das Orderbuch und platziert Limit-Orders auf beiden Marktseiten, um die Geld-Brief-Spanne zu erfassen.

## Strategielogik
- Abonnieren von Echtzeit-Orderbuch-Updates.
- Wenn keine Position offen ist und die Spanne groß genug ist (bestimmt durch `SpreadMultiplier`), platziert die Strategie:
  - Eine Kauf-Limit-Order einen Tick über dem besten Geldkurs.
  - Eine Verkauf-Limit-Order einen Tick unter dem besten Briefkurs.
- Wenn eine Position besteht und keine aktiven Orders vorhanden sind, wird eine einzelne Limit-Order auf der Gegenseite platziert, um die Position zu schließen und umzukehren.
- Orders werden storniert und ersetzt, wenn sich die besten Kurse bewegen, um sie an der Spitze des Buches zu halten.

## Parameter
- `SpreadMultiplier` – erforderliche Spanne in Ticks, um sowohl Kauf- als auch Verkaufsorders zu platzieren. Standard sind 4 Ticks.
- `Volume` – Order-Volumen. Standard ist 1 Lot.

## Verwendungshinweise
- Entwickelt für Instrumente mit kleinen Tick-Größen, wie Futures an der FORTS-Börse.
- Verwendet ausschließlich Limit-Orders; keine Marktorders werden gesendet, außer vom Schutzmechanismus wenn nötig.
- Ausreichende Liquidität und eine Umgebung mit geringer Latenz für effektiven Betrieb sicherstellen.
