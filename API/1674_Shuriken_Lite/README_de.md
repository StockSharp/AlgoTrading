# Shuriken Lite-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert die Funktionalität des originalen *Shuriken Lite* MQL-Tools. Sie verfolgt ausgeführte Trades auf dem Konto und gruppiert sie nach numerischen Identifikatoren, den sogenannten **magic numbers**. Für jede Gruppe berechnet die Strategie:

- Anzahl der Trades
- Gewinn- und Verlust-Trades
- Gesamtgewinn oder -verlust in Pips
- Profit-Faktor

Die Statistiken werden nach jedem neuen Trade protokolliert, wenn die Punktanzeigedarstellung aktiviert ist.

## Parameter

- **Magic Numbers** — kommagetrennte Liste von Identifikatoren zur Gruppierung von Trades. Jeder Identifikator sollte dem numerischen Wert im Orderkommentar entsprechen.
- **Show Scores** — Protokollierung der Statistiken aktivieren oder deaktivieren.

## Verwendung

1. Legen Sie die gewünschten magic numbers im Parameter fest.
2. Führen Sie die Strategie zusammen mit anderen Strategien aus, die numerische Kommentare in ihre Orders schreiben.
3. Überprüfen Sie das Protokoll auf die aggregierten Performance-Metriken.
