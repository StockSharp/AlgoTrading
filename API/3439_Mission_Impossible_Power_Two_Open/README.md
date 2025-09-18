# Mission Impossible Power Two Open Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader expert advisor “Mission Impossible Power Two Open”. It monitors the direction of the most recently completed candle and opens a new basket of trades in that direction. When price moves against the active basket, the strategy adds new averaging entries according to a fixed pip grid. The volume of each new entry grows with the floating loss of the basket, mimicking the `power`-based sizing rule from the original EA. Exit targets are recalculated after every fill so that the entire basket shares a single take-profit and stop-loss level.

## Trading Logic

1. **Signal detection** – On every finished candle the strategy compares the previous candle’s close with its open.
   - If the previous candle closed above its open, the long signal is active.
   - If it closed below the open, the short signal is active.
   - An inside bar (close equal to open) produces no new basket.
2. **Opening the first trade** – If no grid is active in the signaled direction, the strategy places a market order with the `BaseVolume` size.
3. **Averaging grid** – When a basket exists, the strategy keeps measuring the distance between the last filled price and the current close.
   - For longs a new entry is added once price falls by at least `GridStepPips * PriceStep` below the last fill.
   - For shorts the strategy waits until price rises by the same distance above the last fill.
   - The grid stops adding new positions after `MaxTrades` fills have been reached in the respective direction.
4. **Dynamic volume** – Before sending each new order the strategy computes the unrealized loss of the basket, multiplies it by `Power * 0.0001`, and adds the result to `BaseVolume`. The final size is rounded to the exchange volume step, clamped between the security limits, and capped by `MaxVolume`.
5. **Exit management** – After every fill the strategy recomputes the shared targets for the whole basket:
   - With a single position the take-profit is `TakeProfitFirstPips` away from the entry and the stop-loss is `StopLossPips` away in the opposite direction.
   - With two or more positions both levels are anchored to the volume-weighted average price of the basket, using `TakeProfitNextPips` for the target distance and `StopLossPips` for protection.
   - When price touches either the take-profit or the stop-loss all positions in that direction are closed at market.
6. **Independent baskets** – Long and short grids are tracked independently. The strategy can hold both at the same time when alternating signals arrive.

## Parameters

| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `BaseVolume` | `decimal` | `0.01` | Initial order size for a new basket before scaling. |
| `MaxVolume` | `decimal` | `2` | Hard cap for a single market order after rounding. |
| `Power` | `decimal` | `13` | Multiplier applied to the floating loss when calculating the additive volume for new entries. |
| `StopLossPips` | `int` | `400` | Distance in price steps used for the shared stop-loss. |
| `TakeProfitFirstPips` | `int` | `15` | Take-profit distance for the very first entry in a basket. |
| `TakeProfitNextPips` | `int` | `7` | Take-profit distance for averaged baskets (two or more entries). |
| `GridStepPips` | `int` | `21` | Minimum adverse move (in price steps) before another averaging entry is allowed. |
| `MaxTrades` | `int` | `16` | Maximum number of grid trades per direction. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Candles used for signal generation and basket management. |

## Notes

- Order volumes are always aligned to the instrument’s `VolumeStep`, restricted by the security’s `MinVolume` and `MaxVolume` whenever those limits are available from the trading board.
- Long and short state machines are fully separated, allowing the strategy to maintain hedged baskets when market direction alternates quickly.
- The protective levels are recalculated on every fill and rounded to the nearest `PriceStep`, matching the frequent take-profit modification routine performed in the MetaTrader version.
- No indicator buffers are used; all decisions are based on raw candle data and portfolio information, just like in the source EA.
