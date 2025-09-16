# Color Schaff JCCX Trend Cycle MMRec Duplex Strategy

## Overview
- Recreates the dual-direction Expert Advisor "ColorSchaffJCCXTrendCycle_MMRec_Duplex" from MetaTrader within StockSharp.
- Uses two independent Schaff trend cycle stacks built on Jurik moving averages to detect bullish and bearish reversals.
- Implements a simplified MMRec (money management recommender) module that reduces size after repeated losses.
- Applies separate parameter sets for long and short trades, enabling asymmetric configurations across timeframes and price sources.

## Indicator Pipeline
1. **JCCX approximation** – each price is processed by a Jurik moving average to obtain a detrended series. The detrended series and its absolute value are smoothed again with Jurik averages to approximate the original JCCX oscillator.
2. **MACD layer** – the difference between fast and slow JCCX outputs provides the momentum base.
3. **Double stochastic transform** – rolling min/max windows normalize the MACD momentum and produce the final Schaff trend cycle (STC) value in the range -100..+100.
4. **Phase control** – the `Phase` parameter modulates an internal smoothing factor (0.05–0.95) applied after every stochastic step, emulating Jurik "phase" behaviour.

The indicator stack is executed twice: once for the long block and once for the short block. Each block can use different candle types and price inputs.

## Trading Logic
### Long Block
- **Entry**: when the long STC crosses above zero (current value > 0 and the previous delayed value ≤ 0). Existing short positions are closed first.
- **Exit**: when the long STC falls below zero and long exits are enabled.
- **Stops**: optional stop-loss and take-profit distances (expressed in price steps) are evaluated on every completed candle using candle highs/lows.

### Short Block
- **Entry**: when the short STC crosses below zero (current value < 0 and the delayed value ≥ 0). Any existing long position is flattened before opening a short.
- **Exit**: when the short STC climbs above zero and short exits are enabled.
- **Stops**: symmetrical stop-loss and take-profit checks for short trades.

The `SignalBar` parameter defines how many fully closed candles are skipped before signals are evaluated. A value of `1` reproduces the MetaTrader behaviour of using the previous completed candle.

## Money Management (MMRec)
- Two queues track the most recent trade results for longs and shorts.
- `TotalTrigger` limits the queue length; only the latest N results are considered.
- `LossTrigger` defines how many losses within that queue switch the trade size to `SmallVolume`.
- When the loss threshold is not breached, the strategy uses `NormalVolume`.

## Parameters
| Group | Parameter | Description | Default |
| --- | --- | --- | --- |
| Long | `LongCandleType` | Candle type (timeframe) used for long calculations. | 8 hour timeframe |
| Long | `LongFastLength` | Fast Jurik length inside the long JCCX approximation. | 23 |
| Long | `LongSlowLength` | Slow Jurik length for the long JCCX approximation. | 50 |
| Long | `LongSmoothLength` | Jurik smoothing length applied to the numerator/denominator. | 8 |
| Long | `LongPhase` | Phase parameter translated into a smoothing factor (0.05–0.95). | 100 |
| Long | `LongCycle` | Rolling window length for the stochastic transforms. | 10 |
| Long | `LongSignalBar` | Delay (in bars) before a signal is evaluated. | 1 |
| Long | `LongAppliedPrice` | Price source used for long calculations. | Close |
| Long | `LongAllowOpen` / `LongAllowClose` | Enable/disable long entries or exits. | true |
| Long | `LongTotalTrigger` | Number of recent long trades stored for the MMRec queue. | 5 |
| Long | `LongLossTrigger` | Losses required inside the queue to switch to small volume. | 3 |
| Long | `LongSmallVolume` / `LongNormalVolume` | Reduced and default long trade sizes. | 0.01 / 0.1 |
| Long | `LongStopLoss` / `LongTakeProfit` | Optional stop/take distances in price steps. | 1000 / 2000 |
| Short | Same as long (prefixed with `Short`). | | |

## Risk Notes
- Price steps are retrieved from the current `Security`. Ensure the instrument has a valid `PriceStep` or adjust parameters accordingly.
- Stop-loss and take-profit checks are evaluated on completed candles; intrabar execution quality depends on candle resolution.
- The MMRec module relies on comparing entry and exit prices. In live trading slippage may alter the effective result.

## Usage Tips
- Start with identical long/short settings to emulate the original duplex EA, then experiment with asymmetric timeframes.
- Lower the `SignalBar` to zero for faster responses; increase it to filter noise.
- Optimise `LongPhase`/`ShortPhase` together with the smoothing lengths to fine-tune responsiveness vs. smoothness.
