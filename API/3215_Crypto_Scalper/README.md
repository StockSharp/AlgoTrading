# Crypto Scalper Strategy

The Crypto Scalper strategy reproduces the original MetaTrader expert logic with StockSharp high-level components. It watches for
a bullish or bearish crossover of a fast linear weighted moving average on the primary timeframe and confirms the setup with trend
filters calculated on a higher timeframe. Once the conditions align, the strategy enters using market orders and manages exits
through stop-loss and take-profit distances measured in MetaTrader pips.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Primary Candle` | Candle type processed on the main timeframe. | 1-minute time frame |
| `Higher Candle` | Higher timeframe candle type used for confirmation. | 15-minute time frame |
| `Fast LWMA` | Length of the primary linear weighted moving average. | 8 |
| `Higher Fast MA` | Fast LWMA length on the confirmation timeframe. | 6 |
| `Higher Slow MA` | Slow LWMA length on the confirmation timeframe. | 85 |
| `Momentum Period` | Momentum indicator length applied to higher timeframe candles. | 14 |
| `Momentum Threshold` | Minimal deviation from the reference momentum (MetaTrader baseline 100) required for trading. | 0.3 |
| `Momentum Reference` | Reference level used to emulate MetaTrader momentum scaling. | 100 |
| `Stop Loss (pips)` | Protective stop distance in MetaTrader pips. | 20 |
| `Take Profit (pips)` | Protective profit distance in MetaTrader pips. | 50 |
| `Volume` | Order volume expressed in lots. | 0.01 |
| `MACD Fast` | Fast EMA period for the MACD confirmation. | 12 |
| `MACD Slow` | Slow EMA period for the MACD confirmation. | 26 |
| `MACD Signal` | Signal EMA period for the MACD confirmation. | 9 |

## Trading Logic
1. Subscribe to the primary timeframe and compute an LWMA that reacts to price quickly.
2. Detect an entry when the previous candle crosses the LWMA up (long) or down (short).
3. Confirm the crossover using the higher timeframe filters:
   - Higher fast LWMA must stay above the higher slow LWMA for long entries and below for short entries.
   - MACD histogram (main minus signal) needs to be positive for longs and negative for shorts.
   - Momentum must deviate from the reference level by at least `Momentum Threshold`.
4. Send a market order in the detected direction when no other orders are active and the current position allows it.
5. Monitor subsequent candles and close the position when either the stop-loss or the take-profit price is touched.

## Notes
- The strategy uses StockSharp high-level subscriptions with `Bind`, avoiding manual indicator buffers.
- Protective levels are recalculated on every candle using the security price step. A fallback step of `0.0001` is applied if the
  instrument does not expose a configured price step.
- Only one position is allowed at a time. Subsequent signals are ignored until the existing trade finishes.
- All inline comments inside the C# implementation are written in English as required by the repository guidelines.
