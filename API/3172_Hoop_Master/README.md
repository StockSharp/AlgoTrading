# Hoop Master Strategy

## Overview
- Converted from the MetaTrader 5 expert advisor **"Hoop master 2"** by Vladimir Karputov.
- Builds a breakout box around the current price and arms both buy and sell stop orders every time a new candle closes.
- Automatically mirrors the MT5 behaviour of doubling the lot size after a losing trade and resetting it after a profitable cycle.

## Trading Logic
1. Subscribe to the configured candle series and wait for finished candles only. A new candle acts as the "tick" that re-arms the pending orders.
2. When the strategy is flat:
   - Place a **buy stop** `IndentPips` points above the latest close.
   - Place a **sell stop** `IndentPips` points below the latest close.
   - Convert MetaTrader pips into absolute price units using the instrument `PriceStep` and the fractional-digit adjustment (×10 for 3 or 5 decimal quotes).
3. Each pending order stores its own stop-loss and take-profit levels. Once the order is filled the opposite order is cancelled, and the stored protection is recreated with native exchange orders (`SellStop`/`SellLimit` for longs, `BuyStop`/`BuyLimit` for shorts).
4. If a protective order closes the position, the remaining attached order is cancelled to avoid duplicate exits.
5. Optional trailing stop logic moves the protective stop in the trade's favour once the price has advanced by at least `TrailingStopPips` and the improvement exceeds `TrailingStepPips`.
6. After every flat-to-flat trading cycle the realised PnL is evaluated. A negative cycle multiplies the working volume by `LossMultiplier`; otherwise the volume is reset to the base `Volume`.

## Parameters
| Parameter | Description | Default | Notes |
|-----------|-------------|---------|-------|
| `Volume` | Base order size used when arming new pending orders. | Strategy `Volume` property | Doubles after a losing cycle according to `LossMultiplier`. |
| `StopLossPips` | Stop-loss distance in MetaTrader pips. | `25` | Converted to price using the pip size helper. `0` disables the stop. |
| `TakeProfitPips` | Take-profit distance in MetaTrader pips. | `70` | Converted to price. `0` disables the target. |
| `TrailingStopPips` | Distance between price and the trailing stop. | `0` | Set to `0` to disable trailing behaviour. |
| `TrailingStepPips` | Minimal improvement before the trailing stop is moved. | `5` | Only used when `TrailingStopPips` is greater than zero. |
| `IndentPips` | Offset added to the latest close when arming pending orders. | `15` | Ensures the stop orders sit outside the immediate price noise. |
| `LossMultiplier` | Multiplier applied to the next cycle after a loss. | `2` | Implements the martingale-style position sizing from the MT5 EA. |
| `CandleType` | Candle type/timeframe that triggers re-arming. | `1 hour time frame` | Change to match the chart used in testing. |

## Money Management & Protections
- Every filled entry immediately rebuilds its stop-loss and take-profit as real exchange orders so that protections work even if the strategy disconnects.
- `StartProtection()` is invoked during startup to liquidate stray positions left from previous runs.
- Trailing logic adjusts existing stop orders rather than sending market exits, keeping behaviour consistent with MT5 modifications.

## Implementation Notes
- Follows the high-level StockSharp API: candle subscriptions, `BuyStop`/`SellStop` for entries, and `BuyLimit`/`SellLimit` for take-profit orders.
- All textual comments inside the code are in English, while external documentation (this README and translations) provides detailed descriptions for users.
- MetaTrader pip conversion honours fractional-digit symbols (3 or 5 decimals) by multiplying the broker step by 10, matching the original EA's `m_adjusted_point` logic.
