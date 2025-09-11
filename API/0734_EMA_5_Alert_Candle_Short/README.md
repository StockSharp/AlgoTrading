# EMA 5 Alert Candle Short
[Русский](README_ru.md) | [中文](README_zh.md)

The **EMA 5 Alert Candle Short** strategy waits for three candles that touch the 5-period EMA and then identifies a candle that stays above it. A short is opened when the next candle breaks the alert candle low, with the take-profit placed at a distance equal to the stop loss.

## Details
- **Entry Criteria**: after three EMA-touching candles, short on break of a non-touching candle low.
- **Long/Short**: Short only.
- **Exit Criteria**: stop loss at alert candle high, take-profit at equal distance.
- **Stops**: Yes, based on alert candle range.
- **Default Values**:
  - `EmaPeriod = 5`
  - `RiskPerTrade = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filters**:
  - Category: Breakout
  - Direction: Short
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
