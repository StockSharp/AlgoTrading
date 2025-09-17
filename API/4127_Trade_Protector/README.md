# Trade Protector Strategy

## Overview
This strategy is a faithful StockSharp conversion of the MetaTrader 4 expert advisor "trade_protector-1_0". The original script is not a signal generator: it continuously watches already opened positions and adjusts protective orders as price evolves. The port keeps that behaviour intact by working on top of level1 updates, so it can be attached to any manually traded symbol or to another strategy that opens positions. All protective distances are configured in pips to match the MQL inputs.

## Trading Logic
- Every level1 update records the latest bid/ask prices and checks whether the strategy currently holds a position. If there is no exposure the script waits silently.
- For long positions the algorithm evaluates up to three stop-loss candidates: the existing stop, a proportional stop tied to floating profit, and a classical trailing stop. The proportional stop is calculated as `entry + ratio * (bid - entry) - spread` once the unrealized profit exceeds `ProportionalThresholdPips`. The trailing candidate keeps the stop `TrailingStopPips` behind the bid whenever price is still close to the entry price (`bid < entry + 4 * spread`).
- The selected long stop is the highest valid candidate. Before sending the order the price is nudged below the bid by at least one tick to avoid immediate execution.
- Short positions mirror the same approach. The proportional stop becomes `entry - ratio * (entry - ask) + spread` after the price moves favourably, and the trailing candidate is placed at `ask + TrailingStopPips + spread` when the market is close to the entry. The strategy always keeps the stop above the ask.
- The escape block emulates the "bail out" mode from the MetaTrader version. When a long trade experiences a drawdown larger than `EscapeLevelPips` (plus five ticks) the strategy arms a take-profit at `entry + EscapeTakeProfitPips`. The short side places the take-profit at `entry - EscapeTakeProfitPips` after a symmetric loss. Negative take-profit values are allowed to exit the trade at a smaller loss.
- Stop and take-profit orders are re-registered whenever a new level is calculated. Active orders are cancelled automatically if their price or volume needs to change, guaranteeing that only one protective order per side remains on the exchange.

## Parameters
| Name | Description |
| --- | --- |
| `TrailingStopPips` | Base trailing distance in pips that anchors the stop behind the market. |
| `ProportionalThresholdPips` | Minimum floating profit (in pips) required before proportional trailing is considered. |
| `ProportionalRatio` | Fraction of the current profit used to build the proportional stop-loss. |
| `UseEscape` | Enables the drawdown-based escape logic and the associated take-profit order. |
| `EscapeLevelPips` | Loss in pips that arms the escape take-profit. A value of zero replicates the default EA behaviour (activate after a five-tick loss). |
| `EscapeTakeProfitPips` | Distance between the entry price and the escape take-profit. Can be negative to target a partial loss. |
| `EnableDetailedLogging` | When enabled the strategy writes informational log entries every time a protective order is moved. |

## Conversion Notes
- Pip distances are converted using an adjusted point calculation identical to the MetaTrader `Point` variable, including the 3/5-digit handling. The same value is reused for the escape calculations so that the trigger thresholds match the original script.
- The strategy works purely with level1 data, replicating the continuous monitoring loop from the MQL `start()` function without relying on candles or indicators.
- Protective orders are created through StockSharp high-level helpers (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`). This keeps the implementation close to the original idea of modifying existing orders while still using idiomatic StockSharp APIs.
- File logging from the MQL version is replaced with optional `LogInfo` calls controlled by `EnableDetailedLogging`. This keeps the code concise while still providing detailed feedback during debugging.
- The escape take-profit is not removed once armed, mirroring the MetaTrader behaviour where the TP stays in place even if price later leaves the drawdown area.
