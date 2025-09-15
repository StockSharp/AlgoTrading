# SAW System 1 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This breakout strategy places stop orders at the start of each trading day. It measures the average daily range over a configurable number of days and uses that value to derive stop-loss and take-profit levels. Orders are positioned on both sides of the current price and only one side is expected to trigger.

At the specified `OpenHour` the strategy calculates buy and sell stop prices at half of the stop-loss distance from the current market price. The stop-loss and take-profit levels are defined as percentages of the average range. When one stop order is filled the opposite order can either be cancelled or kept for position reversal. An optional martingale feature multiplies the volume of the remaining order after a fill.

Any pending entry orders that remain unfilled by `CloseHour` are removed to avoid overnight exposure. After an entry the strategy immediately places protective stop-loss and take-profit orders relative to the fill price.

## Details

- **Entry Criteria:**
  - Calculate average daily range using an ATR over `VolatilityDays` days.
  - Compute stop-loss and take-profit distances as `StopLossRate` and `TakeProfitRate` percent of that range.
  - At `OpenHour` place buy and sell stop orders `offset = stopLoss/2` away from market price.
- **Exit Criteria:**
  - Protective stop-loss and take-profit orders close positions.
  - Pending entry orders are cancelled at `CloseHour`.
- **Reverse Mode:**
  - If `Reverse` is true the opposite stop order remains to reverse the position.
  - If `UseMartingale` is also true the remaining order is re-registered with volume multiplied by `MartingaleMultiplier`.
- **Long/Short:** Both directions.
- **Stops:** Fixed stop-loss and take-profit based on daily range.
- **Default Values:**
  - `VolatilityDays` = 5
  - `OpenHour` = 7
  - `CloseHour` = 10
  - `StopLossRate` = 15%
  - `TakeProfitRate` = 30%
  - `Reverse` = false
  - `UseMartingale` = false
  - `MartingaleMultiplier` = 2.0

This approach attempts to capture breakouts after quiet overnight sessions while limiting risk through volatility-adjusted targets.
