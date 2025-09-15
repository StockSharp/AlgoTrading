# Opening and Closing on Time v2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A time-based strategy that opens trades at a specific time and closes them later in the day. Trade direction is confirmed by comparing a fast and a slow exponential moving average. Stop-loss and take-profit levels are expressed in ticks.

## Details

- **Entry Criteria**: At `OpenTime`, go long if the fast EMA is above the slow EMA, go short if it is below. Direction depends on `TradeMode`.
- **Long/Short**: Configurable (buy, sell, or both).
- **Exit Criteria**: Positions are closed at `CloseTime` or by protective stops.
- **Stops**: Yes, both stop-loss and take-profit in ticks.
- **Default Values**:
  - `OpenTime` = 05:00
  - `CloseTime` = 21:01
  - `SlowPeriod` = 200
  - `FastPeriod` = 50
  - `StopLossTicks` = 30
  - `TakeProfitTicks` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Time Based
  - Direction: Configurable
  - Indicators: EMA
  - Stops: Fixed
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
