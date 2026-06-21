# Flash Minervini-Qualifier-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert EMA-Kreuzung, SuperTrend und Momentum-RSI mit Minervini-Phasenanalyse zur Qualifizierung von Trades.

## Details

- **Einstiegskriterien**: EMA über der Trailing-Linie, SuperTrend-Trend und Momentum-RSI über Schwellenwert mit Minervini-Phasenfilter
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetzter Trailing oder SuperTrend-Umkehr
- **Stops**: Nein
- **Standardwerte**:
  - `MomRsiLength` = 10
  - `MomRsiThreshold` = 60
  - `EmaLength` = 12
  - `EmaPercent` = 0.01
  - `SuperTrendPeriod` = 10
  - `SuperTrendMultiplier` = 3
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, SuperTrend, RSI
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
