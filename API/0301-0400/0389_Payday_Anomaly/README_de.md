# Zahltag-Anomalie-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie nutzt den "Zahltag"-Effekt, indem sie um typische Gehaltszahlungstermine einen breiten Markt-ETF hält. Der ETF wird ab zwei Handelstagen vor Monatsende bis zum dritten Handelstag des neuen Monats gehalten, um die Zuflüsse aus Gehaltseinzahlungen zu erfassen.

Den Rest des Monats ist das Portfolio in Cash. Tageskerzen bestimmen das Fenster und Marktaufträge passen die Position an.

## Details

- **Instrument**: breiter Markt-ETF.
- **Fenster**: von zwei Tagen vor Monatsende bis zum dritten Handelstag des nächsten Monats.
- **Positionierung**: Long während des Fensters, sonst ohne Position.
- **Daten**: Tageskerzen.
- **Risikokontrolle**: Handel übersprungen, wenn der Auftragswert unter `MinTradeUsd` liegt.
