# Doji Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy looks for Doji candles appearing above an exponential moving average. When such a pattern occurs, it enters a long position. The stop loss is set to the lowest low of recent bars and a trailing stop protects profit after the price moves enough in favor.

## Details

- **Entry Criteria**: Doji candle with close above EMA.
- **Long/Short**: Long only.
- **Exit Criteria**: Lowest low stop and trailing stop.
- **Stops**: Yes, fixed and trailing.
- **Default Values**:
  - `CandleType` = 5 minute
  - `EmaLength` = 60
  - `Tolerance` = 0.05
  - `StopBars` = 450
  - `TrailTriggerPercent` = 1
  - `TrailOffsetPercent` = 0.5
- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: EMA, Candlestick
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
