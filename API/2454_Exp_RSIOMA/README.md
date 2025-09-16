# Exp RSIOMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Exp RSIOMA strategy uses the RSI of moving average (RSIOMA) indicator to trade trend reversals and breakouts. RSI values are smoothed by an additional moving average to form a signal line and histogram. The strategy supports four modes:

1. **Breakdown** – trades when RSI crosses configured high/low levels.
2. **HistTwist** – trades when histogram changes direction.
3. **SignalTwist** – trades when the signal line changes direction.
4. **HistDisposition** – trades when histogram crosses the signal line.

Positions can be opened or closed independently for long and short sides.

## Details

- **Entry Criteria**: depends on `Mode`
- **Long/Short**: both
- **Exit Criteria**: opposite signal
- **Stops**: none
- **Default Values**:
  - `CandleType` = 4 hour
  - `RsiPeriod` = 14
  - `SignalPeriod` = 21
  - `HighLevel` = 20
  - `LowLevel` = -20
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: RSI
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
