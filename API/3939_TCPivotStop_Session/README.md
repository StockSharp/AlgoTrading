# TCPivot Session Stop Strategy

## Overview

The TCPivot Session Stop strategy is a direct port of the MetaTrader 4 expert advisor `gpfTCPivotStop`. It trades around the classic daily pivot level calculated from the previous trading day. The strategy:

- Calculates the pivot point, three resistance, and three support levels from the prior day's high, low, and close.
- Waits for the current close to cross the pivot level from below (long setup) or from above (short setup).
- Opens a market position in the direction of the breakout and assigns a stop-loss and take-profit at the selected pivot level tier.
- Optionally forces the position to close at the beginning of a specified session hour to emulate the original intraday exit.

The implementation is based on the StockSharp high level API. Positions are sized with the `Volume` property of the base `Strategy` class.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `TargetLevel` | Pivot tier used for stop-loss and take-profit (1, 2, or 3). | `1` |
| `CloseAtSessionStart` | If enabled, closes open positions when the configured hour begins. | `false` |
| `SessionCloseHour` | Session hour (0-23) evaluated when `CloseAtSessionStart` is enabled. | `0` |
| `CandleType` | Time frame that feeds the trading signals. | `H1` |

## Trading Logic

1. Subscribe to hourly (or configured) candles for signals and daily candles for pivot calculation.
2. At the completion of each daily candle, compute the classical pivot levels:
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 * Pivot - Low`, `S1 = 2 * Pivot - High`
   - `R2 = Pivot + (R1 - S1)`, `S2 = Pivot - (R1 - S1)`
   - `R3 = High + 2 * (Pivot - Low)`, `S3 = Low - 2 * (High - Pivot)`
3. When a signal candle finishes:
   - If `CloseAtSessionStart` is enabled and the candle opens at `SessionCloseHour`, immediately flatten the position.
   - If flat and the previous close was below the pivot while the current close is above it, enter long with target/stop selected by `TargetLevel`.
   - If flat and the previous close was above the pivot while the current close is below it, enter short with the mirrored target/stop.
   - If already in a position, exit when the close touches the configured stop-loss or take-profit level.

## Notes

- The strategy uses `StartProtection()` to integrate with the platform's built-in risk controls. Stop-loss and take-profit exits are handled explicitly inside the strategy logic.
- The MetaTrader version included optional email notifications and dynamic position sizing based on account risk. These features are not part of the StockSharp port; use the platform's notification and money management modules if needed.
- The original expert advisor closed trades at midnight when `isTradeDay` was enabled. This behavior is reproduced through `CloseAtSessionStart` + `SessionCloseHour` (set to `0` to mimic midnight).
