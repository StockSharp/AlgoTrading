# X2MA JJRSX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die einen dualen gleitenden Durchschnitts-Trendfilter mit einem RSI-basierten Einstiegs-Trigger kombiniert.
Der Trend wird auf einem höheren Zeitrahmen durch den Vergleich eines schnellen und eines langsamen gleitenden Durchschnitts bestimmt.
Einstiege werden auf einem niedrigeren Zeitrahmen ausgeführt, wenn der RSI in Trendrichtung die überkauften oder überverkauften Zonen verlässt.

## Details

- **Einstiegskriterien**:
  - Long: Trend aufwärts und RSI kreuzt über `Oversold`
  - Short: Trend abwärts und RSI kreuzt unter `Overbought`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetzter RSI-Schwellenwert oder Trendumkehr
- **Stops**: Keine
- **Standardwerte**:
  - `TrendCandleType` = 4h-Kerzen
  - `SignalCandleType` = 30m-Kerzen
  - `FastMaPeriod` = 12
  - `SlowMaPeriod` = 5
  - `RsiPeriod` = 8
  - `Overbought` = 70
  - `Oversold` = 30
