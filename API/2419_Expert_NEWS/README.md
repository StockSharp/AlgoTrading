# Expert NEWS Strategy

## Overview
Expert NEWS Strategy is a direct conversion of the "Expert_NEWS" MQL5 robot. The strategy continuously places symmetrical stop orders above and below the current market price and manages the resulting positions with break-even protection, trailing stops, and scheduled refreshes of pending orders. The implementation relies on Level1 quotes and keeps the default trading volume at 0.1 lots.

## Trading Logic
1. **Quote subscription** – the strategy listens to best bid/ask updates and computes order prices from the latest values.
2. **Initial stop orders** – when no long position or buy stop exists, a new buy stop is placed at `ask + EntryOffsetTicks * PriceStep`. When no short position or sell stop exists, a sell stop is placed at `bid - EntryOffsetTicks * PriceStep`.
3. **Order refreshing** – every `OrderRefreshSeconds`, the strategy cancels and re-creates a pending stop if the required price deviates by more than `TrailingStepTicks` ticks.
4. **Position protection** – after a fill, the strategy opens protective stop and take-profit orders if the requested distances meet the `MinimumStopTicks` constraint.
5. **Break-even control** – when `UseBreakEven` is enabled, the stop is pulled to `entry ± BreakEvenProfitTicks` once the market moves far enough and the new stop respects the minimum distance from the current quote.
6. **Trailing stop** – once the price advances by `TrailingStartTicks`, the stop follows using `TrailingStopTicks` as the distance and `TrailingStepTicks` as the minimum improvement step.
7. **Cleanup** – flattening the position cancels every remaining protective order.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `StopLossTicks` | Initial protective stop distance (ticks). Set to zero to disable the initial stop order. |
| `TakeProfitTicks` | Initial take-profit distance (ticks). Set to zero to disable the target order. |
| `TrailingStopTicks` | Distance of the trailing stop (ticks). |
| `TrailingStartTicks` | Profit in ticks required before the trailing logic activates. |
| `TrailingStepTicks` | Minimum improvement when refreshing either the trailing stop or the pending entry orders. |
| `UseBreakEven` | Enables the break-even shift of the stop once there is enough profit. |
| `BreakEvenProfitTicks` | Additional profit cushion when moving the stop to break-even. |
| `EntryOffsetTicks` | Distance between current quote and each new stop entry order. |
| `OrderRefreshSeconds` | Time interval between automatic refresh attempts for pending stop orders. |
| `MinimumStopTicks` | Manual fallback for the broker stop-level requirement. Stops closer than this distance are not submitted. |

## Position Management
- Protective orders always match the net position volume. Partial fills automatically resize the stop and take-profit orders.
- Break-even and trailing logic work even when the initial stop is disabled; the stop will be created dynamically once the rules are satisfied.
- The strategy keeps the most recent stop price in memory so that trailing updates preserve monotonic behavior.

## Usage Notes
- Ensure the `Security.PriceStep` is configured; every tick distance parameter is multiplied by this value.
- The default volume is `0.1` to mirror the original robot. Adjust the `Volume` property if another size is required.
- `MinimumStopTicks` should be set to the venue’s stop-level requirement if the trading venue enforces one. Leave it at zero to allow the tightest possible stops.
- The algorithm does not rely on historical bars and can operate on streaming quotes only.
