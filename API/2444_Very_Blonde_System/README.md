# Very Blonde System Strategy

Grid-based counter-trend strategy inspired by the original "Very Blonde System" for MetaTrader. The strategy looks for a large distance between the current price and recent extremes and trades in the opposite direction.

## Strategy Logic
1. Calculate the highest high and lowest low over the last *Count Bars* candles.
2. When there are no open positions:
   - If the distance from the recent high to the current price exceeds *Limit* ticks, buy at market.
   - If the distance from the current price to the recent low exceeds *Limit* ticks, sell at market.
   - After entering a position, place four additional limit orders every *Grid* ticks away, doubling the volume on each level.
3. When a position exists:
   - If the total profit exceeds *Amount* currency units, close the position and cancel all pending orders.
   - If *Lock Down* is greater than zero, once price moves in favor by that many ticks the strategy activates a breakeven protection. If price returns to the entry level, all positions are closed.

## Parameters
| Name | Description |
|------|-------------|
| `CountBars` | Number of candles to search for highs and lows. |
| `Limit` | Minimum distance from the extreme in ticks to open a trade. |
| `Grid` | Distance in ticks between additional grid orders. |
| `Amount` | Target profit in currency to close all positions. |
| `LockDown` | Distance in ticks to enable breakeven protection. |
| `CandleType` | Candle type used for calculations. |

The strategy uses market orders for initial entries and limit orders for grid levels. All comments in the code are written in English as required.
