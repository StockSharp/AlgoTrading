# Momentum-Stil-Rotations-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Python-Strategie rotiert zwischen einer Reihe von Faktor-ETFs und einem breiten Markt-ETF. Am Ende jedes Monats werden die ETFs nach ihrer kumulierten Drei-Monats-Rendite gerankt. Das Portfolio investiert dann vollständig in den bestplatzierten Fonds für den folgenden Monat, um mittelfristiges Momentum zu nutzen.

Der Ansatz hält stets einen einzigen ETF und bewertet ihn monatlich neu. Tageskerzen werden für Berechnungen verwendet und alle Neugewichtungsgeschäfte werden zum Marktpreis ausgeführt.

## Details

- **Universum**: Liste von Faktor-ETFs und einem Benchmark-ETF.
- **Signal**: 63-Tage-Gesamtrendite (drei Monate) berechnen und das stärkste Instrument auswählen.
- **Neugewichtung**: erster Handelstag jedes Monats.
- **Positionierung**: vollständig Long im ausgewählten ETF, alle anderen ohne Position.
- **Risikokontrolle**: Aufträge werden übersprungen, wenn der erforderliche Handelswert unter `MinTradeUsd` fällt.
