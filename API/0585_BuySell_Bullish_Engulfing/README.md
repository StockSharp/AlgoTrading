# Buy & Sell Bullish Engulfing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long when a bullish candle fully engulfs the previous bearish bar and optional trend conditions are met. Position size is a percentage of current equity, while take profit and stop loss close trades automatically.

## Details

- **Entry Criteria**: Bullish engulfing pattern with optional SMA trend filter.
- **Long/Short**: Long only.
- **Exit Criteria**: Take profit or stop loss.
- **Stops**: Yes, both take profit and stop loss.
- **Default Values**:
  - `CandleType` = 15 minute
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
  - `OrderPercent` = 30
  - `TrendMode` = SMA50
- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: Candlestick, SMA
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
