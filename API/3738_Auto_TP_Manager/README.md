# Auto TP Manager Strategy

## Overview
The **Auto TP Manager Strategy** ports the MetaTrader 5 expert advisor *Auto Tp.mq5* to StockSharp. The strategy does not open its own trades. Instead, it monitors the current net position on the selected security and automatically attaches take-profit and stop-loss orders. Optional features include a pip-based trailing stop and an equity-protection circuit that liquidates the position when the account drawdown exceeds a configurable percentage.

The logic is designed for discretionary or external trading flows where orders are placed manually or by third-party systems. As soon as the strategy detects a non-zero position, it calculates the required protection levels and registers the corresponding orders through StockSharp's high-level API.

## Core Behaviour
- Detects manually opened positions and attaches fresh protective orders once per position change.
- Places a take-profit order at a fixed pip distance from the entry price.
- Optionally places a stop-loss order and keeps it synchronized with the latest volume and average entry price.
- Supports trailing the stop-loss once the price moves into profit by the configured distance.
- Tracks account equity and closes the position if equity falls below the specified percentage of the initial balance.
- Cancels outstanding protection orders when the position is fully closed.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `TakeProfitPips` | Take-profit distance in pips calculated from the average entry price. | `25` |
| `UseStopLoss` | Enables or disables the stop-loss component. | `false` |
| `StopLossPips` | Initial stop-loss distance in pips. Active only when `UseStopLoss` is `true`. | `12` |
| `UseTrailingStop` | Enables the trailing logic that moves the stop in the trade's favor. Requires `UseStopLoss = true`. | `false` |
| `TrailingStopPips` | Distance maintained by the trailing stop in pips. | `15` |
| `UseEquityProtection` | Activates the equity watchdog that forces an exit when equity drops too far. | `false` |
| `MinEquityPercent` | Minimum equity level as a percentage of the starting balance. Once breached, the current position is closed. | `20` |
| `Slippage` | Reserved for compatibility with the original EA. Not used directly by the ported strategy. | `3` |

The strategy automatically discovers the pip size using the instrument's `PriceStep`. For 5-digit FX pairs the MQL behaviour is replicated by multiplying the step by ten.

## Trailing Stop Logic
When `UseTrailingStop` and `UseStopLoss` are both enabled, the strategy begins trailing once the market moves in the trade's favour beyond the configured pip distance. For long positions, the best bid price is used; for short positions, the best ask price. The trailing stop never steps backwards and always maintains the requested gap between price and stop order.

## Equity Protection
Equity protection is computed using `Portfolio.BeginValue` as the baseline and `Portfolio.CurrentValue` for the live equity figure. If the current equity falls below `MinEquityPercent` of the initial balance, the strategy sends a `ClosePosition()` request and leaves the market.

## Usage Notes
1. Attach the strategy to a security and portfolio, then start it before entering trades manually.
2. Confirm that the account supports stop and limit orders because the strategy registers both order types.
3. Trailing requires live level-one data (best bid/ask). Ensure the data feed supplies those fields.
4. Slippage handling must be configured on the broker side; the parameter is preserved only for parity with the MQL input.

## Conversion Details
- Source file: `MQL/61119/Auto_Tp.mq5`.
- Platform: StockSharp high-level API with `SubscribeLevel1()` bindings.
- Enhancements: added equity guardrails, extensive parameter metadata, and descriptive logging via strategy properties.

The port keeps the original trading rules intact while following the StockSharp guidelines for parameter handling, binding, and risk management helpers.
