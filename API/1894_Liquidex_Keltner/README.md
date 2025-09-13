# Liquidex Keltner
[Русский](README_ru.md) | [中文](README_cn.md)

The **Liquidex Keltner** strategy trades breakouts of Keltner Channels with a moving average trend filter.
Trades are allowed only during specified hours and can optionally be confirmed by RSI direction.
Stop-loss and take-profit are managed using fixed percentages.

## Details
- **Entry Criteria**:
  - Price crosses above the upper Keltner band and closes above the moving average.
  - Price crosses below the lower Keltner band and closes below the moving average.
  - Candle body must exceed `RangeFilter`.
  - When `UseRsiFilter` is enabled, RSI must be above 50 for longs and below 50 for shorts.
  - Current time must be between `EntryHourFrom` and `EntryHourTo`, and before `FridayEndHour` on Fridays.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop-loss or take-profit.
- **Stops**: Yes, percentage-based via `StartProtection`.
- **Default Values**:
  - `MaPeriod = 7`
  - `RangeFilter = 10m`
  - `StopLoss = 1m`
  - `TakeProfit = 2m`
  - `UseKeltnerFilter = true`
  - `KeltnerPeriod = 6`
  - `KeltnerMultiplier = 1m`
  - `UseRsiFilter = false`
  - `RsiPeriod = 14`
  - `EntryHourFrom = 2`
  - `EntryHourTo = 24`
  - `FridayEndHour = 22`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: MA, Keltner, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (15m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
