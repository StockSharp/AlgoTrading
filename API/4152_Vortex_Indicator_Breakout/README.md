# Vortex Indicator Breakout Strategy

This strategy ports the MetaTrader expert **Vortex Indicator System.mq4** into the StockSharp high level API. The original idea wa
s published in *Technical Analysis of Stocks & Commodities* (January 2010) and relies on the Vortex indicator crossover to arm bre
akout orders at the high/low of the crossover candle. The StockSharp version keeps the same decision flow: a crossover closes the
opposite position, arms a breakout trigger at the crossover bar extreme, and the next candle that breaks that level executes the
market order.

## How it works

1. A single candle subscription is opened according to `CandleType`. The resulting stream is bound to one `VortexIndicator` insta
nce using `Bind`, so the strategy always receives synchronized VI+ and VI- values for the finished candles.
2. When the indicator finishes warming up, the algorithm tracks the previous VI values to detect the same crossover conditions u
sed in the MQL expert: `VI+` crossing above `VI-` or vice versa between the last two closed candles.
3. **Setup phase** – as soon as a bullish crossover is detected, any open short position is closed immediately and the high of th
e crossover candle becomes the pending long trigger. The opposite crossover closes an existing long position and stores the low 
of that bar as the short trigger.
4. **Trigger phase** – on each subsequent finished candle the strategy checks whether the recorded trigger price was touched (`Hi
ghPrice` ≥ long trigger or `LowPrice` ≤ short trigger). If so, it submits a market order sized to both flatten the remaining oppo
site exposure (if the previous order was not completed yet) and open a new position with `TradeVolume`.
5. Once an order fires, the corresponding trigger is cleared. If no breakout happens the setup stays active until a new crossove
r overrides it.
6. Exits rely exclusively on the crossover logic: the opposite signal immediately flattens the current position and arms a new b
reakout trigger, mirroring the MetaTrader implementation.

## Signals

- **Bullish setup** – occurs when `VI+` was below or equal to `VI-` on the previous closed candle and rises above it on the most r
ecent one. The long trigger is set to that candle’s high.
- **Bullish execution** – the next candle whose high reaches the trigger sends a market buy order using `TradeVolume` (plus any vo
lume required to close an outstanding short position).
- **Bearish setup** – occurs when `VI-` was below or equal to `VI+` on the previous closed candle and rises above it on the most r
ecent one. The short trigger is set to that candle’s low.
- **Bearish execution** – the next candle whose low touches the trigger sends a market sell order using `TradeVolume` (plus the vo
lume necessary to flatten an open long position).

## Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `VortexLength` | 14 | Period applied to the Vortex indicator. |
| `CandleType` | 1 hour | Timeframe used for candles and indicator updates. |
| `TradeVolume` | 1 | Market order size used for new entries. |

## Implementation notes

- The strategy only reacts to **finished** candles to comply with the conversion guidelines. Intrabar breakouts are recognised a
s soon as a candle closes with a high/low beyond the stored trigger.
- Pending triggers are cleared on `OnStopped` so the instance can be restarted cleanly without leftover state.
- When executing a breakout order the algorithm increases the volume if it still holds an opposite position, achieving the same e
ffect as the MetaTrader expert, which closed the active order before opening the new one.
