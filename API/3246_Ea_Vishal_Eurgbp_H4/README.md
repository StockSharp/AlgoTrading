# EA Vishal EURGBP H4 Strategy

## Overview
The **EA Vishal EURGBP H4 Strategy** replicates the original MetaTrader expert advisor that combines a stochastic crossover entry filter with envelope-based exits. The logic operates on H4 candles by default and uses virtual risk management tools (stop-loss, take-profit, and optional trailing stop) defined in pips, closely mirroring the MT4 behaviour.

## Trading Logic
- **Entry** – the strategy waits for a stochastic crossover evaluated on the two most recent completed candles. A long position is opened when %K crosses below %D between bar *n-2* and *n-1*. A short position is opened on the opposite crossover. Only one position can be active at a time.
- **Exit** – active positions are managed in three layers:
  1. **Envelope breakout** – if the next bar opens beyond the previous envelope band while the earlier bar opened inside, the position is closed immediately.
  2. **Virtual stop-loss / take-profit** – target prices are computed from the entry price using the configured pip distances.
  3. **Optional trailing stop** – when enabled and a stop-loss is defined, the stop level trails the highest (for longs) or lowest (for shorts) value of the previous candle minus/plus the stop distance.

## Parameters
| Name | Default | Description |
| ---- | ------- | ----------- |
| `Volume` | 0.5 | Order volume in lots for every trade. |
| `StopLossPips` | 0 | Hard stop-loss distance in pips (0 disables the stop). |
| `TakeProfitPips` | 22 | Take-profit distance in pips (0 disables the target). |
| `UseTrailingStop` | false | Enables the virtual trailing stop that follows the previous candle’s extremum. Requires `StopLossPips` &gt; 0. |
| `StochasticKPeriod` | 6 | Lookback period for the stochastic %K calculation. |
| `StochasticDPeriod` | 3 | Smoothing period for the %D line. |
| `StochasticSlowing` | 1 | Slowing factor applied to %K. |
| `EnvelopePeriod` | 32 | Length of the SMA used as the envelope basis. |
| `EnvelopeDeviationPercent` | 0.3 | Deviation in percent applied above/below the SMA to build the envelopes. |
| `CandleType` | H4 time frame | Candle series that feeds the strategy (default is four-hour candles). |

## Notes
- All parameters are exposed for optimisation in StockSharp Studio.
- Protective levels are tracked internally and executed with market orders when the candle range pierces them, matching the behaviour of the original expert advisor on new bar events.
- The strategy relies on finished candles only, ensuring deterministic backtests and production behaviour.
