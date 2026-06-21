# X Trail 2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des Kreuzungspunkts zweier konfigurierbarer gleitender Durchschnitte, die aus einem gewählten Preistyp berechnet werden.

## Details
- **Einstieg**: Kauft, wenn MA1 MA2 von unten kreuzt und dieser Kreuzungspunkt durch die vorherigen zwei Balken bestätigt wird; verkauft bei umgekehrtem Signal.
- **Ausstieg**: Entgegengesetzter Kreuzungspunkt.
- **Indikatoren**: Zwei gleitende Durchschnitte mit wählbarem Typ (simple, exponential, smoothed, weighted) und Preisquelle (close, open, high, low, median, typical, weighted).
- **Parameter**:
  - `Ma1Length` = 1
  - `Ma1Type` = MovingAverageTypeEnum.Simple
  - `Ma1PriceType` = AppliedPriceType.Median
  - `Ma2Length` = 14
  - `Ma2Type` = MovingAverageTypeEnum.Simple
  - `Ma2PriceType` = AppliedPriceType.Median
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
