# Intra Bullish Profit Ping v4.0-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nur-Long-System mit EMA-Kreuzung, bestätigt durch MACD-Histogramm und RSI-Stärke.

## Details

- **Einstiegskriterien**:
  - Kurze EMA kreuzt über die lange EMA
  - MACD-Histogramm > 0
  - RSI > 50
  - Schluss > Eröffnung
- **Ausstiegskriterien**:
  - Kurze EMA kreuzt unter die lange EMA
  - MACD-Histogramm < 0
  - RSI < 50
  - Schluss < Eröffnung
- **Indikatoren**:
  - Exponentielle gleitende Durchschnitte
  - MACD
  - RSI
- **Stops**: Keine.
- **Standardwerte**:
  - `ShortEmaLength` = 7
  - `LongEmaLength` = 14
  - `RsiLength` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
- **Filter**:
  - Trendfolge
  - Einzelner Zeitrahmen
  - Indikatoren: EMA, MACD, RSI
  - Stops: Keine
  - Komplexität: Niedrig
