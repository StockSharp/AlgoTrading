# MultiMartinStrategy

## Overview

`MultiMartinStrategy` is the StockSharp conversion of the MQL5 expert advisor **MultiMartin**. The original robot is a multi-currency martingale that alternates long and short trades on reversal signals and grows the order size after losing deals. This port keeps the core money-management logic while using StockSharp's high-level API for order routing, position monitoring, optional trailing stops, and broker rejection handling.

The strategy continuously opens a single market position on the configured instrument. After every exit it either keeps the direction (if the trade was profitable) or flips the direction (if the trade lost money). Losing trades trigger a martingale step that multiplies the next order volume until a configurable ceiling is reached.

## Trading logic

1. **Entry selection**
   - The strategy uses a time filter to limit trading to an intraday window. Outside of this window no new entries are submitted.
   - When no position is open and the broker is not in a cooldown state, the strategy sends a market order in the current direction. The first direction is user-defined (buy or sell).
2. **Martingale sizing**
   - After each loss the next order volume is multiplied by the `Factor` parameter.
   - The multiplication is capped by `Limit`, which defines the maximum number of consecutive doublings. Once the cap is exceeded the volume resets to the base `Volume`.
   - Profitable trades always reset the volume to the base size and keep the trade direction.
3. **Exit management**
   - Stop-loss and take-profit distances are expressed in price points and converted to absolute distances using the instrument `PriceStep`.
   - Optional trailing modes move the stop-loss to breakeven or trail it linearly behind the price.
   - Exits are handled by market orders once the candle extremes breach either the stop or take threshold.
4. **Broker rejection handling**
   - If an order is rejected the strategy enters a cooldown period controlled by `SkipBadTime`. During cooldown no new entries are attempted. The `Forever` option disables trading for the remainder of the session.

## Parameters

| Name | Description |
| --- | --- |
| `UseTimeFilter` | Enable or disable the intraday trading window. |
| `HourStart` | Inclusive hour (0-23) when trading becomes active. |
| `HourEnd` | Exclusive hour (1-24) when trading stops. Supports overnight windows (e.g. 22-2). |
| `Volume` | Base order volume in lots or contracts. |
| `Factor` | Multiplier applied to the next order volume after a losing trade. |
| `Limit` | Maximum number of consecutive multiplications before the volume resets. |
| `StopLossPoints` | Stop-loss distance expressed in instrument points. Set to 0 to disable the stop. |
| `TakeProfitPoints` | Take-profit distance expressed in instrument points. Set to 0 to disable the target. |
| `StartDirection` | First trade direction (`Buy` or `Sell`). |
| `SkipBadTime` | Cooldown interval applied after a rejected market order. `Forever` blocks further entries. |
| `TrailMode` | Trailing mode: `None`, `Breakeven`, or `Straight` (linear trailing). |
| `CandleType` | Candle series used for managing exits and time filtering. |

## Differences versus the MQL5 version

- The StockSharp port trades a single security per strategy instance. Launch multiple instances to cover multiple symbols.
- Stop-loss and take-profit management is candle-based; fills are executed with market orders as soon as the candle range touches the thresholds.
- Broker rejections use StockSharp's `OnOrderFailed` callback to trigger the `SkipBadTime` cooldown instead of MQL5's global timer.
- Trailing stop options were reimplemented using strategy-level logic instead of direct order modification calls.

## Usage notes

- Configure the `Security` and `Portfolio` before starting the strategy.
- Ensure that `Volume` is compatible with the instrument's lot size and fractional volume rules.
- Set `StopLossPoints`/`TakeProfitPoints` to zero to deactivate the respective protective orders.
- When backtesting, choose a candle type that matches the historical dataset (e.g., 1-minute candles for forex pairs).
- To simulate the original multi-symbol behaviour, deploy multiple strategy instances with different securities and parameters.

## Risk warnings

Martingale money management is inherently risky. Losing streaks can grow the exposure exponentially and consume available margin quickly. Use conservative volume settings, test on historical data, and apply strict risk controls before using the strategy in production.
