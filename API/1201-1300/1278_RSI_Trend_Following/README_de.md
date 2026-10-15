# RSI-Trendfolge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die RSI-Trendfolge-Strategie geht long, wenn der Momentum durch RSI, Stochastik, MACD und einen Preis oberhalb eines langfristigen EMA bestätigt wird. Ein Trailing-Stop wird nach einer günstigen ATR-Bewegung aktiviert und folgt einem kürzeren EMA.

Positionen werden geschlossen, wenn der Preis unter den Trailing-EMA fällt oder den ATR-basierten Stop-Loss erreicht.

## Details

- **Einstiegskriterien**: `K < 80 && D < 80 && MACD > Signal && RSI > 50 && Low > EMA(200)`
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Preis unter Trailing-EMA oder Stop-Loss
- **Stops**: Ja, ATR-basiert
- **Standardwerte**:
  - `StopLossAtr` = 1.75
  - `TrailingActivationAtr` = 2.25
  - `RsiPeriod` = 14
  - `TrailingEmaLength` = 20
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
