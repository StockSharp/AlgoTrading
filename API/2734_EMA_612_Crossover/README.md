# Ema612CrossoverStrategy

## Summary
- Port of the MetaTrader 5 expert advisor **"EMA 6.12 (barabashkakvn's edition)"** into the StockSharp high level API.
- Trades the crossover between a fast and a slow simple moving average (the original script also used MODE_SMA despite its EMA name).
- Adds optional take profit and trailing stop management expressed in absolute price units so the behaviour can be tuned per instrument.

## Trading logic
### Data preparation
- The strategy subscribes to candles of the type defined by `CandleType` (15 minute timeframe by default).
- Two simple moving averages are calculated: `FastPeriod` length for the fast curve and `SlowPeriod` length for the slow curve. The slow period must be greater than the fast period.

### Entry rules
- Signals are evaluated on the close of each finished candle.
- A **bullish crossover** occurs when the slow SMA was above the fast SMA on the previous candle and drops below it on the current candle. Any open short position is closed and a long position is opened with the configured `Volume`.
- A **bearish crossover** occurs when the slow SMA was below the fast SMA on the previous candle and rises above it on the current candle. Any open long position is closed and a short position is opened with the configured `Volume`.

### Exit rules
- Open positions are closed on the opposite crossover as described above.
- Optional take profit: if `TakeProfitOffset` is greater than zero, the strategy calculates a fixed price target from the entry price. Long trades exit when price reaches `entry + TakeProfitOffset`; short trades exit when price reaches `entry - TakeProfitOffset`.
- Optional trailing stop: when `TrailingStopOffset` is greater than zero the strategy waits until unrealised profit exceeds `TrailingStopOffset + TrailingStepOffset`. Once that threshold is crossed the stop price is tightened to stay `TrailingStopOffset` away from the latest close, but only if the new level is at least `TrailingStepOffset` closer to price than the previous stop. Long trades use lows to trigger the stop, shorts use highs.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `CandleType` | 15 minute time frame | Candle resolution used for SMA calculations and signal evaluation. |
| `FastPeriod` | 6 | Period for the fast simple moving average. Must be > 0 and less than `SlowPeriod`. |
| `SlowPeriod` | 54 | Period for the slow simple moving average. Must be > 0 and greater than `FastPeriod`. |
| `Volume` | 1 | Order volume used for new entries. |
| `TakeProfitOffset` | 0.001 | Optional absolute price distance for the take profit target. Set to 0 to disable. |
| `TrailingStopOffset` | 0.005 | Absolute distance between price and trailing stop. Set to 0 to disable trailing. |
| `TrailingStepOffset` | 0.0005 | Additional favourable move required before the trailing stop is moved. |

> **Important:** the offsets are specified in absolute price units. Adjust them to match the instrument's tick size (for example, on EURUSD with a 0.0001 step the defaults correspond to 10, 50 and 5 pips respectively).

## Implementation notes
- Uses the high level `SubscribeCandles().Bind()` workflow as required by the project guidelines.
- Chart output plots both SMAs and trade markers when charting is available in the environment.
- State variables track the entry price, trailing stop level and take profit target exactly like the MQL version.
- The C# implementation enforces `SlowPeriod > FastPeriod` at start-up to avoid invalid indicator configuration.

## Usage tips
- Optimise the candle timeframe and SMA periods to match the market being traded (e.g., shorter periods for intraday futures, longer for swing trading).
- Convert the offsets from pips or ticks into absolute price units before running the strategy.
- Trailing can be deactivated by setting `TrailingStopOffset` to zero; the strategy will then rely solely on the opposite crossover or the optional take profit for exits.
