# Pendulum Strategy

## Overview
The **Pendulum Strategy** is a StockSharp port of the MetaTrader expert advisor *Pendulum 1_01*. The original system keeps two pending stop orders around the current price and progressively increases their volume after each fill. This C# version reproduces the same "swing" behaviour using high-level StockSharp helpers.

Key ideas:

- Maintain symmetric buy-stop and sell-stop orders at a configurable distance from the last closed candle.
- After each fill, the next stop on the same side multiplies its volume, implementing the martingale-style progression from the MQL source.
- Close the position when a short-term pip target is reached or when the account equity crosses global profit/loss thresholds.

## How it works
1. When the strategy starts, it subscribes to a user-defined candle series (default: 15 minutes) and, optionally, to daily candles. The daily range controls the distance between the market price and the pending stops.
2. On every finished trading candle the algorithm:
   - Updates global equity-based limits.
   - Verifies whether the current position reached the local profit target.
   - Calculates the stop distance either from the latest daily range or from the manual pip input, and then places/upgrades the buy-stop and sell-stop orders.
3. When a stop order is filled the corresponding progression level advances, so the next stop on that side uses the multiplied volume. Once `MaxLevels` is reached no new orders are created for that direction until the position returns to zero.
4. Global take-profit / stop-loss checks run after every candle and liquidate the portfolio if the configured equity thresholds are breached.

## Parameters
| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `BaseVolume` | `decimal` | `0.1` | Volume of the first pending stop. |
| `VolumeMultiplier` | `decimal` | `2` | Factor applied after each filled level on the same side. |
| `MaxLevels` | `int` | `8` | Maximum number of fills allowed in one direction. |
| `ManualStepPips` | `int` | `50` | Stop distance in pips when the daily range is unavailable. |
| `UseDynamicRange` | `bool` | `true` | If enabled, derives the step from the latest finished daily candle. |
| `RangeFraction` | `decimal` | `0.2` | Fraction of the daily range used as the base stop distance. |
| `TakeProfitPips` | `int` | `10` | Local pip target that closes the current position. Set `0` to disable. |
| `SlippagePips` | `int` | `3` | Extra buffer added to the pending distance to mimic MetaTrader slippage. |
| `UseGlobalTargets` | `bool` | `true` | Enables equity-based liquidation checks. |
| `GlobalTakePercent` | `decimal` | `1` | Equity growth (in percent) that triggers global take-profit. |
| `GlobalStopPercent` | `decimal` | `2` | Equity drawdown (in percent) that triggers global stop-loss. |
| `CandleType` | `DataType` | `15m` candles | Timeframe used for the primary trading logic. |

## Notes
- Position sizing respects the instrument volume step, minimum and maximum volume settings.
- Stop prices snap to the instrument price step and avoid constant order replacement by honouring a price tolerance.
- Global targets rely on `Portfolio.CurrentValue` (or `BeginValue` as a fallback), so the selected portfolio must expose this information.
- The strategy uses `StartProtection()` to activate StockSharp's built-in position protection once at start-up.

## Conversion differences
- UI label drawing and account balance tables from the original MQL script are omitted.
- Global take-profit levels follow percentage-based equity thresholds instead of the raw tick-value arithmetic used in MQL, keeping the behaviour consistent across brokers.
- MetaTrader-specific functions such as `OrderModify` are replaced with StockSharp order cancellation and re-submission routines.
