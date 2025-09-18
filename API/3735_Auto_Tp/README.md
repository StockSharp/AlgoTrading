# Auto TP Strategy

The **Auto TP Strategy** is a risk-management helper converted from the MetaTrader 4 "Auto_Tp" expert advisor. It does not generate trade entries by itself. Instead, it watches the strategy position and automatically attaches protective orders:

- Places a fixed take-profit at a configurable distance in MetaTrader pips.
- Optionally applies an initial stop-loss.
- Can trail the stop-loss once price moves in favour of the trade.
- Monitors account equity and closes all open positions if equity falls under a configurable percentage of balance.

## How it works

1. When the strategy position changes from flat to long/short, the strategy immediately attaches the protective take-profit and, if enabled, the stop-loss to the new volume.
2. While the position remains open, incoming Level1 ticks are used to trail the stop for the active side. Trailing is only enabled when both the stop-loss and the trailing option are active.
3. On every tick the current equity is compared with the configured minimum. When the account equity drops below the threshold, the strategy flattens all positions and prevents repeated triggers until equity recovers above the threshold.

The strategy uses the instrument price step to determine the MetaTrader pip size. For 3- and 5-decimal instruments (or 2/3 decimals for JPY pairs) the pip is defined as ten price steps, matching the behaviour of the original EA.

## Parameters

| Name | Description |
| --- | --- |
| `TakeProfitPips` | Distance of the take-profit order in MetaTrader pips. Must be positive. |
| `UseStopLoss` | Enables placement of the stop-loss order. |
| `StopLossPips` | Distance of the stop-loss order in MetaTrader pips. Used only when `UseStopLoss` is enabled. |
| `UseTrailingStop` | Enables trailing of the stop-loss when the trade moves into profit. Requires `UseStopLoss`. |
| `TrailingStopPips` | Trailing distance in MetaTrader pips. |
| `UseEquityProtection` | Enables global equity protection. |
| `MinEquityPercent` | Minimum allowed equity expressed as a percentage of the account balance. When equity falls below this percentage the strategy closes all trades. |

## Usage notes

- The strategy should be combined with another entry module or manual trade placement. Only positions opened through the StockSharp strategy are protected.
- Trailing uses the best bid for long positions and the best ask for short positions. Stops are always adjusted away from the market by at least one price step to avoid immediate execution.
- Equity protection uses `Portfolio.BeginValue` as the reference balance when available; otherwise it falls back to the current equity snapshot.
- When the position size changes (partial close or pyramiding), existing protective orders are re-synchronised to the new average entry price and volume.

## Differences from the original EA

- StockSharp market mechanics require explicit stop and limit orders instead of modifying existing MetaTrader positions. Protective orders are cancelled and re-registered when parameters change.
- Equity protection uses portfolio values provided by the connected broker. The MetaTrader version relied on the terminal's AccountBalance and AccountEquity functions.
- Slippage control when force-closing trades is handled by the broker; the explicit `Slippage` input from the EA is not required in StockSharp and therefore omitted.

## Charting

This utility does not add custom chart elements. You may combine it with any entry strategy and chart layout if visualisation is required.

