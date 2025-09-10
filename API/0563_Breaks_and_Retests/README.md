# Breaks and Retests
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy entering on breakouts of recent highs or lows and optional retests with trailing stop management.

The approach first tracks support and resistance defined by the highest and lowest closes over a lookback window. Breakouts open positions in the breakout direction or wait for a retest of the broken level. Exits use an initial stop loss that turns into a trailing stop once profit reaches a threshold.

## Details

- **Entry Criteria**: Breakout above resistance or below support, optional retest.
- **Long/Short**: Configurable.
- **Exit Criteria**: Trailing stop or opposite breakout.
- **Stops**: Initial stop loss and trailing stop.
- **Default Values**:
  - `LookbackPeriod` = 20
  - `RetestBarsSinceBreakout` = 2
  - `RetestDetectionLimit` = 2
  - `ProfitThresholdPercent` = 5m
  - `TrailingStopGapPercent` = 1m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Highest, Lowest
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
