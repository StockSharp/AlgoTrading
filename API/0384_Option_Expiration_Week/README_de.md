# Optionsverfall-Wochen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Python-Strategie kauft und hält einen Aktien-ETF nur während der Optionsverfallwoche. Ab dem Montag vor dem dritten Freitag jedes Monats wird der ETF gekauft und die Position wird beim Freitagsschluss geschlossen. Die Idee nutzt die kurzfristige Stärke, die oft in der Verfallwoche beobachtet wird.

Außerhalb dieses Fensters bleibt das Portfolio in Cash. Tageskerzen werden verwendet und Geschäfte werden einmal täglich als Marktaufträge gesendet.

## Details

- **Instrument**: einzelner Aktien-ETF.
- **Signal**: Kalenderregel für die Woche, die am dritten Freitag endet.
- **Halteperiode**: Montagsöffnung bis Freitagsschluss der Verfallwoche.
- **Positionierung**: vollständig investiert während des Fensters, sonst ohne Position.
- **Risikokontrolle**: Handel übersprungen, wenn der Auftragswert unter `MinTradeUsd` liegt.
