# RSI und ATR Trendumkehr-Strategie mit SL TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die RSI und ATR verwendet, um Trendumkehrungen mit dynamischen Stop-Loss- und Take-Profit-Niveaus zu erkennen.

## Details

- **Einstiegskriterien**: Preis kreuzt adaptiven RSI/ATR-Schwellenwert.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetzter Kreuzung.
- **Stops**: Integriert über dynamischen Schwellenwert.
- **Standardwerte**:
  - `RsiLength` = 8
  - `RsiMultiplier` = 1.5
  - `Lookback` = 1
  - `MinDifference` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: RSI, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
