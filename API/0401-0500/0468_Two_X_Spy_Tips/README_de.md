# Strategie Two X SPY TIPS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie investiert Kapital in den gehandelten Vermögenswert, wenn sowohl S&P 500 als auch TIPS-Preise zu Beginn eines neuen Monats über ihren gleitenden 200-Perioden-Durchschnitten liegen.

## Details

- **Einstiegskriterien**: S&P 500 und TIPS über ihrer SMA bei einem neuen Monat.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Keine Ausstiege.
- **Stops**: Nein.
- **Standardwerte**:
  - `SmaLength` = 200
  - `Leverage` = 2
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
