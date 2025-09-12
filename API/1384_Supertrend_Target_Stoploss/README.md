# Supertrend Target Stop
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that buys when price crosses above the Supertrend line and sells when it crosses below. A fixed percentage target and stop loss close positions.

## Details

- **Entry Criteria**: Price crossing Supertrend.
- **Long/Short**: Both directions.
- **Exit Criteria**: Target or stop loss percentage.
- **Stops**: Yes, fixed percentage.
- **Default Values**:
  - `Period` = 14
  - `Multiplier` = 3m
  - `TargetPct` = 0.01m
  - `StopPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR, Supertrend
  - Stops: Fixed
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
