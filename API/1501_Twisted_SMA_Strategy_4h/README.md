# Twisted SMA Strategy 4h
[Русский](README_ru.md) | [中文](README_cn.md)

The Twisted SMA Strategy uses three simple moving averages and a KAMA filter on 4 -hour candles. A long position is opened when the fast SMA is above the middle SMA, the middle above the slow, price above a longer SMA and the KAMA is not flat. The position closes when the SMAs align bearish.

## Details

- **Entry Criteria**: fast SMA > mid SMA > slow SMA, close > main SMA, KAMA not flat.
- **Long/Short**: Long only.
- **Exit Criteria**: fast SMA < mid SMA < slow SMA.
- **Stops**: No.
- **Default Values**:
  - `FastLength` = 4
  - `MidLength` = 9
  - `SlowLength` = 18
  - `MainSmaLength` = 100
  - `KamaLength` = 25
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: SMA, KAMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
