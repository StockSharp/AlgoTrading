# Consecutive Bearish Candle
[Русский](README_ru.md) | [中文](README_cn.md)

Enters long after a run of bearish candles and exits when price breaks above the previous high.

This mean reversion approach buys after excessive downside pressure, seeking a rebound once sellers are exhausted.

## Details

- **Entry Criteria**: `N` consecutive bearish candles within the time window.
- **Long/Short**: Long only.
- **Exit Criteria**: Close above previous high.
- **Stops**: No.
- **Default Values**:
  - `Lookback` = 3
  - `CandleType` = TimeSpan.FromDays(1)
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
- **Filters**:
  - Category: Mean Reversion
  - Direction: Long
  - Indicators: Price Action
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

