# Fraktal-Ausbruch-Trendfolge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Fraktal-Ausbruch-Trendfolge steigt mit einer Kauf-Stop-Order über einem aktivierten bullishen Fraktal ein, wenn die Volatilität niedrig ist.

## Details

- **Einstiegskriterien**: Aufwärtsfraktal über den Alligator-Zähnen und gemitteltes ATR-Perzentil unter dem Schwellenwert; Kauf-Stop auf Fraktalniveau.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Stop-Loss beim Höheren aus prozentualem Stop oder Abwärtsfraktal-Aktivierung.
- **Stops**: Ja.
- **Standardwerte**:
  - `StopLossPercent` = 0.03
  - `AtrThreshold` = 50
  - `AtrPeriod` = 5
  - `CandleType` = TimeSpan.FromHours(1)
  - `TradeStart` = 2023-01-01
  - `TradeStop` = 2025-01-01
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: Fractal, SMMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
