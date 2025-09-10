# Bearish Wick Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy buys when a bearish candle forms a long lower wick that exceeds a user-defined percentage threshold. An optional EMA filter requires the close to be above a moving average to confirm trend direction. Positions are closed when price closes above the previous candle's high.

## Details

- **Entry Criteria:** bearish candle with lower wick <= threshold and within trading window; optionally price above EMA.
- **Long/Short:** Long only.
- **Exit Criteria:** close price > previous high.
- **Stops:** None.
- **Default Values:**
  - Threshold = -1 (%)
  - EMA filter disabled, EMA period = 200
  - Start time = 2014-01-01, End time = 2099-01-01
  - Candle timeframe = 1 minute
- **Filters:**
  - Category: Reversal
  - Direction: Long
  - Indicators: EMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
