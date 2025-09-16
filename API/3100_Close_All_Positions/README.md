# 3100 Close All Positions

## Overview
- Converts the MQL5 utility **Close all positions** into a StockSharp high-level strategy.
- Watches finished candles of the configured timeframe and accumulates the floating profit of every open position across the assigned portfolio.
- When the floating profit equals or exceeds the threshold, market orders are sent to flatten all securities handled by the strategy (including child strategies) until the book is fully closed.
- The `_closeAllRequested` flag mirrors the MQL `m_close_all` variable so that exit orders continue to be issued until no positions remain.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `ProfitThreshold` | `decimal` | `10` | Floating profit (in account currency) required before the strategy flattens every open position. Mirrors `InpProfit` from the EA. |
| `CandleType` | `DataType` | `1m` timeframe | Candle series that defines the "new bar" moments. The profit check is executed only when a candle finishes, emulating the original `PrevBars` logic. |

## Trading Logic
1. The strategy subscribes to candles of `CandleType` and processes only finished bars, just like the EA evaluated profit only on a new bar.
2. On each finished bar the helper `CalculateTotalProfit` retrieves `Portfolio.CurrentProfit` (floating PnL including commission and swap). If the adapter cannot provide this value it falls back to summing individual position `PnL` values.
3. If the calculated floating profit is below `ProfitThreshold`, nothing happens.
4. As soon as the profit meets the threshold, `_closeAllRequested` is set to `true` and `CloseAllPositions()` is executed immediately.
5. `CloseAllPositions()` collects every security that has an exposure in the portfolio or in nested strategies and sends market orders in the opposite direction of the current volume (long → sell, short → buy).
6. The `_closeAllRequested` flag remains set until `HasAnyOpenPosition()` detects that the portfolio is flat, matching the MQL behaviour where `m_close_all` stayed true until all tickets were closed.

## Additional Notes
- Only the C# implementation is provided; the Python folder is intentionally left empty per the task requirements.
- The strategy does not cancel pending orders because the original script only closed market positions.
- Use `SetOptimize` on `ProfitThreshold` to explore alternative profit targets through the Designer optimizer if needed.

## Files
- `CS/CloseAllPositionsStrategy.cs`
