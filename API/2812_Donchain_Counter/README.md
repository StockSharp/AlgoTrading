# Donchain Counter Strategy

## Overview
The Donchain Counter strategy is a StockSharp port of the MQL5 expert advisor "Donchain counter" by Michal Rutka. The system watches how the Donchian Channel expands to detect breakouts and then defends the position by trailing the stop along the opposite band once price has moved a fixed distance away. Only one position can be opened every 24 hours, mirroring the original constraint.

## Trading Logic
### Long Entries
- Evaluate signals on completed candles of the configured timeframe (default **H1**).
- Observe the upper Donchian band on the previous two closed bars. When the band on bar *t-1* is higher than on bar *t-2* (a fresh breakout of the channel high), a long market order is placed.
- The initial protective stop is anchored to the current lower Donchian band.

### Short Entries
- Monitor the lower Donchian band on the previous two closed bars. When the band on bar *t-1* is lower than on bar *t-2* (a breakout of the channel low), a short market order is submitted.
- The first stop level is set to the current upper Donchian band.

### Trade Cooldown
- After any new entry the algorithm records the execution time and blocks subsequent entries for the duration of `TradeCooldown` (default **24 hours**). This reproduces the “only one trade per day” rule in the MQL version.

### Trailing and Exit Rules
- A trailing mechanism engages only after price advances at least `BufferSteps` price steps beyond the opposite Donchian band. This replicates the requirement from the original EA where the market must move 50 points before the stop is tightened.
- Long positions: once the trailing trigger fires, the stop is updated to the current lower band. If the candle’s low touches that level the strategy exits with a market order.
- Short positions: after the trigger fires, the stop follows the current upper band. If the candle’s high reaches that price the position is closed.
- When the trailing stop forces an exit the strategy does not open a new position until the next signal and the cooldown permit it.

### Risk Handling
- The strategy always trades a single position whose size is defined by the `Volume` parameter.
- There is no profit target; all exits are driven by the Donchian trailing logic.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Volume` | Order size for entries. | `1` |
| `ChannelPeriod` | Lookback period for the Donchian Channel calculation. | `20` |
| `BufferSteps` | Number of price steps price must exceed beyond the opposite band before trailing activates (MQL used 50 points). | `50` |
| `TradeCooldown` | Minimum time between new entries. | `1 day` |
| `CandleType` | Candle series used for the indicator (default 1-hour candles). | `1h candles` |

## Indicators
- **Donchian Channels** – upper and lower bands define breakout signals and dynamic stops.

## Notes
- Use instruments with a sensible `PriceStep` so the buffer translates to realistic price distance. The strategy defaults to a 0.0001 step if none is provided by the security.
- Only one direction is open at a time. Before flipping direction the existing position must fully close, just like the original expert advisor.
- Chart objects are automatically prepared if a chart area is available: candles, the Donchian channel and the strategy’s own trades.
