# Supertrend-Strategie (5m)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Supertrend-Strategie auf 5-Minuten-Kerzen.

## Details

- **Einstiegskriterien**: Preis kreuzt den Supertrend von unten nach oben.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Preis kreuzt den Supertrend von oben nach unten.
- **Stops**: Nein.
- **Standardwerte**:
  - `AtrPeriod` = 10
  - `Multiplier` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: ATR, Supertrend
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
