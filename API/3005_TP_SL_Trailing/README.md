# TP SL Trailing Strategy

## Overview
This strategy is a direct conversion of the MetaTrader 5 expert advisor "TP SL Trailing". The strategy does not generate entries by itself. Instead, it manages existing positions by installing a protective stop-loss and take-profit and by trailing the stop once the trade becomes profitable. The pip-based configuration matches the parameters of the original script and allows the logic to be attached to any symbol supported by StockSharp.

## Trading Logic
- When a new position appears, the strategy can optionally set an initial stop-loss and take-profit using the configured pip distances. This behavior is controlled by the **Only Zero Values** flag, just as in the original expert advisor.
- For long positions, the strategy moves the stop-loss upward once the unrealized profit exceeds the sum of the trailing stop and the trailing step. The stop is moved to `current price - trailing stop`, guaranteeing that a minimum portion of the profit is locked in.
- For short positions, the strategy mirrors the same idea and moves the stop downward once the profit exceeds the trailing thresholds.
- If both the trailing stop and the trailing step are zero, the strategy leaves the stop-loss untouched.
- The take-profit level is never trailed. It is only set during the initial placement phase when **Only Zero Values** is enabled, fully replicating the MQL behavior.

## Parameters
| Name | Description |
| --- | --- |
| `CandleType` | Timeframe of the candles used to track price movements. A faster timeframe improves trailing accuracy. |
| `StopLossPips` | Distance in pips between the entry price and the initial stop-loss. Applied only when **Only Zero Values** is enabled. |
| `TakeProfitPips` | Distance in pips between the entry price and the initial take-profit. Applied only when **Only Zero Values** is enabled. |
| `TrailingStopPips` | Core trailing distance in pips. Defines how far behind the current price the stop should remain after activation. |
| `TrailingStepPips` | Additional pip buffer that must be exceeded before the stop moves again. Prevents over-frequent stop updates. |
| `OnlyZeroValues` | Matches the original EA flag. When enabled, initial protective orders are created only for positions that currently have no stop-loss or take-profit assigned. |

## Conversion Notes
- Pip distances are converted to price units using the security's `PriceStep`. This keeps the logic instrument-agnostic and mirrors the 3/5-digit adjustment in the MQL version.
- Protective orders are re-registered whenever the trailing logic moves the stop-loss. Active orders from a previous position are cancelled automatically when the position size returns to zero.
- All code comments are written in English, while this documentation is intentionally detailed to help reproduce every decision made during the porting process.
