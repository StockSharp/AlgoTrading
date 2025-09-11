# CE XAU/USDT Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy goes long when the close price crosses above its SMA and goes short when the close crosses below the SMA.

## Details

- **Entry Criteria:** close crosses above SMA for long, below for short.
- **Long/Short:** Both.
- **Exit Criteria:** reverse signal.
- **Stops:** None.
- **Default Values:**
  - SMA period = 14
  - Candle timeframe = 1 minute
- **Filters:**
  - Category: Trend
  - Direction: Long & Short
  - Indicators: SMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
