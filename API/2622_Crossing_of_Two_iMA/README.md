# Crossing of Two iMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy ports the classic **“Crossing of two iMA”** MetaTrader 5 expert advisor into the StockSharp high-level API. It trades when two configurable moving averages cross and can optionally require confirmation from a third moving average that acts as a directional filter. The implementation keeps the original flexibility by supporting manual or risk-based position sizing, pending-entry style offsets and a trailing stop with user-defined step.

The conversion processes signals on the close of each finished candle, replicating how the MQL5 expert waits for a new bar. Pending order behavior (`PriceLevelPips`) is simulated internally by monitoring candle highs and lows, so no actual stop/limit orders are submitted. A long pending trigger activates when the bar reaches the chosen price for buy stop entries or dips to the price for buy limit entries, and the same symmetric logic is applied for short setups.

## Trading rules

- **Indicators**
  - `First` moving average (period, shift and method are configurable).
  - `Second` moving average (also fully configurable).
  - Optional `Third` moving average used as a filter (`UseThirdMovingAverage = true`).
- **Entry conditions**
  - **Primary cross (bars 0 and 1)**
    - **Long**: first MA crosses above the second MA on the current bar while it was below on the previous bar. If the filter is active, the third MA must stay below the first MA to validate the long breakout.
    - **Short**: first MA crosses below the second MA and, if the filter is enabled, the third MA must stay above the first MA.
  - **Fallback cross (bars 0 and 2)**
    - Performs an additional lookback to catch quick crosses that occurred between the previous two bars. The strategy ignores this signal if another trade was already opened within the last three bars (same as the MQL5 history lookup).
- **Direction**: both long and short.
- **Stops and targets**
  - Stop loss and take profit are expressed in pips. They are converted to price offsets based on the instrument tick size and adjusted for 3/5 digit pricing just like the original EA.
  - Trailing stop activates only when `TrailingStopPips > 0`. It moves the stop by the trailing distance once price advances by at least `TrailingStepPips` beyond the previous stop level.
- **Pending order mode (`PriceLevelPips`)**
  - `0`: enter immediately at market.
  - `< 0`: simulate stop orders (buy stop above price, sell stop below price). The stop loss and take profit are shifted by the same offset.
  - `> 0`: simulate limit orders (buy limit below price, sell limit above price). Protective levels are shifted accordingly.

## Money management

- `UseFixedVolume = true` replicates the EA’s manual lot mode. The strategy simply uses `Volume` (and closes opposite positions before opening a new one).
- When `UseFixedVolume = false`, the strategy allocates risk as `Portfolio.CurrentValue * RiskPercent / 100`. The order size becomes `riskAmount / stopDistance`. If no stop loss is provided (`StopLossPips = 0`), the calculated risk distance equals zero, so the strategy refuses to open a position—identical to the original `MoneyFixedRisk` behavior returning zero lots.

## Trailing logic

- Long positions trail the stop to `Close - TrailingStopPips * pipValue` once price has moved by at least `TrailingStepPips` beyond the previous stop. The trailing value always moves upward and never loosens the stop.
- Short positions mirror this behavior by moving the stop to `Close + TrailingStopPips * pipValue` when the price advances enough in favor.
- Take profit and initial stop are checked before trailing adjustments, ensuring exits match the original EA priorities.

## Default parameters

- First MA: length `5`, shift `3`, method `Smoothed`.
- Second MA: length `8`, shift `5`, method `Smoothed`.
- Third MA filter: enabled, length `13`, shift `8`, method `Smoothed`.
- Risk controls: stop loss `50` pips, take profit `50` pips, trailing `10` pips with a `4` pip step.
- Money management: `UseFixedVolume = true`, `RiskPercent = 5` for the alternative sizing mode.
- Pending offset: `0` pips (market execution).
- Candle type: 1-minute time frame (can be changed to match the original chart period).

## Implementation notes

- The moving average `shift` parameters delay signal values exactly by the configured number of bars, so plotting on StockSharp charts matches the MT5 visual shift.
- The strategy stores only the minimal state required (current, previous and two bars back) to satisfy the “bars [0], [1], [2]” logic from MQL5. No historical collections are recreated beyond that buffer.
- Pending entries are cleared whenever a new signal appears, replicating the EA’s `DeleteAllOrders()` call.
- Because StockSharp executes orders asynchronously, the entry price recorded for trailing and target calculations uses the intended trigger price. Backtests therefore reproduce the original EA logic on candle data without relying on tick-level fills.
