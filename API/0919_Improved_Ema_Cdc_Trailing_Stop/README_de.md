# Verbesserte EMA & CDC Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert EMA-Trendfilter, MACD-Bestätigung und einen ATR-basierten CDC Trailing-Stop.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis > EMA60, EMA60 > EMA90, MACD-Linie > Signallinie.
  - **Short**: Preis < EMA60, EMA60 < EMA90, MACD-Linie < Signallinie.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Trailing-Stop oder ATR-basiertes Gewinnziel.
- **Stops**: Ja.
- **Standardwerte**:
  - `Ema60Period` = 60
  - `Ema90Period` = 90
  - `AtrPeriod` = 24
  - `Multiplier` = 4
  - `ProfitTargetMultiplier` = 2
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, MACD, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
