# Uptrick X PineIndicators: Z-Score Flow-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Trendfolge-Strategie mit Z-Score-, EMA- und RSI-Filtern.

## Details

- **Einstiegskriterien**: Z-Score kreuzt Kauf-/Verkaufsschwellen mit Trend- und RSI-Bestätigung
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal basierend auf dem gewählten Modus
- **Stops**: Nein
- **Standardwerte**:
  - `ZScorePeriod` = 100
  - `EmaTrendLen` = 50
  - `RsiLen` = 14
  - `RsiEmaLen` = 8
  - `ZBuyLevel` = -2
  - `ZSellLevel` = 2
  - `CooldownBars` = 10
  - `SlopeIndex` = 30
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA, EMA, RSI, StandardDeviation
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
