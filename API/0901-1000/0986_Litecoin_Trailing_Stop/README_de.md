# Litecoin Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Litecoin Trailing-Stop-Strategie** verwendet den Kaufman Adaptive Moving Average (KAMA), um bullische und bärische Trends zu erkennen. Sie eröffnet Long-Positionen, wenn KAMA steigt, und Short-Positionen, wenn er fällt. Nach einer konfigurierbaren Verzögerung schützt ein prozentbasierter Trailing-Stop die Gewinne.

## Details
- **Einstiegskriterien**: KAMA-Steigung mit Abkühlung zwischen Einstiegen.
- **Long/Short**: beide Richtungen.
- **Ausstiegskriterien**: Trailing-Stop.
- **Stops**: Trailing-Stop nach Verzögerung.
- **Standardwerte**:
  - `KamaLength = 50`
  - `BarsBetweenEntries = 30`
  - `TrailingStopPercent = 12`
  - `DelayBars = 50`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: KAMA
  - Stops: Trailing
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
