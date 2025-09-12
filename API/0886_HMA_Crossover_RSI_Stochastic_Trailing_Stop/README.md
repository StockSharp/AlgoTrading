# HMA Crossover RSI Stochastic Trailing Stop
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using crossover of fast and slow HMA with RSI and smoothed Stochastic filter. It opens long when the fast HMA crosses above the slow HMA with RSI and Stochastic below thresholds, and opens short on the opposite condition. A trailing stop manages exits.

## Details

- **Entry Criteria**: Fast HMA cross above slow HMA with RSI and Stochastic below thresholds.
- **Long/Short**: Both directions.
- **Exit Criteria**: Trailing stop or opposite signal.
- **Stops**: Trailing percent.
- **Default Values**:
  - `FastHmaLength` = 5
  - `SlowHmaLength` = 20
  - `RsiPeriod` = 14
  - `RsiBuyLevel` = 45
  - `RsiSellLevel` = 60
  - `StochLength` = 14
  - `StochSmooth` = 3
  - `StochBuyLevel` = 39
  - `StochSellLevel` = 63
  - `TrailingPercent` = 5
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: HMA, RSI, Stochastic
  - Stops: Trailing
  - Complexity: Basic
  - Timeframe: 1h
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
