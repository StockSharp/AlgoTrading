# Supertrend Nur-Long-Strategie für QQQ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nur-Long-Strategie basierend auf dem Supertrend-Indikator und einem Datumsbereichsfilter.

## Details

- **Einstiegskriterien**: Preis kreuzt den Supertrend von unten nach oben.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Preis kreuzt den Supertrend von oben nach unten.
- **Stops**: Nein.
- **Standardwerte**:
  - `AtrPeriod` = 32
  - `Multiplier` = 4.35m
  - `StartDate` = 1995-01-01
  - `EndDate` = 2050-01-01
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: ATR, Supertrend
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
