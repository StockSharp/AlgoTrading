# Billy Expert Strategy

## Overview
- Converted from the original MetaTrader 4 expert "Billy_expert.mq4".
- Long-only momentum strategy that waits for four consecutive descending highs and opens before entering.
- Uses two stochastic oscillators (fast on the trading timeframe, slow on a higher timeframe) to confirm that momentum is shifting upward.
- Designed for spot FX pairs but can be applied to any instrument that provides minute-based candles.

## Signal logic
### Price action filter
1. Evaluate finished candles on the primary timeframe.
2. Require four consecutive candles where both the high and the open decrease. This recreates the MT4 `High[0] < High[1] < High[2] < High[3]` and `Open[0] < Open[1] < Open[2] < Open[3]` checks.
3. The pattern suggests an exhausted bearish move and prepares the strategy for a reversal trade.

### Oscillator confirmation
1. Calculate a fast stochastic oscillator on the trading timeframe and a slow stochastic on the confirmation timeframe.
2. For each oscillator, demand that the %K line be above the %D line on both the current and previous completed candle (`%K(0) > %D(0)` and `%K(1) > %D(1)`).
3. The trade is triggered only when both oscillators simultaneously confirm bullish momentum.

## Order management
- Entries: market buys sized by the strategy `Volume` parameter (if a short position exists it is closed and reversed automatically).
- Stop loss: fixed distance below the fill price using the `Stop Loss (pts)` parameter. A value of `0` disables the stop.
- Take profit: fixed distance above the fill price using the `Take Profit (pts)` parameter. A value of `0` disables the target.
- Position cap: `Max Orders` limits how many long entries can be active at the same time. Because StockSharp keeps a net position, the strategy approximates the MT4 behaviour by counting how many `Volume` blocks are currently open.
- Trailing stop: the original EA declared a trailing stop input but did not implement it. The converted version also omits trailing logic for parity.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Trading Candle` | Primary timeframe for price pattern and fast stochastic. | 1 minute |
| `Slow Stochastic Candle` | Higher timeframe used for the confirmation stochastic. | 5 minutes |
| `Stochastic Length` | Lookback window for %K. | 5 |
| `%K Smoothing` | Smoothing applied to the %K line. | 3 |
| `%D Period` | Smoothing applied to the %D line. | 3 |
| `Slowing` | Additional smoothing factor for %K. | 3 |
| `Stop Loss (pts)` | Stop loss distance in price steps. | 0 |
| `Take Profit (pts)` | Take profit distance in price steps. | 12 |
| `Max Orders` | Maximum simultaneous long entries. | 1 |

## Usage notes
- Set the `Volume` property before starting the strategy; StockSharp defaults to `0`, which would block order placement.
- The price step is read from `Security.PriceStep` (falls back to `Security.Step` or `1`). Ensure your instrument metadata is configured correctly to get precise stop/target levels.
- When the confirmation timeframe differs from the trading timeframe, the most recent completed slow candle is reused until a new one appears, matching the behaviour of the original MT4 script.
- The EA did not manage exits beyond broker-side stop loss and take profit. The conversion mirrors this behaviour by sending protective market orders when the levels are touched.
- Because StockSharp aggregates positions, `Max Orders > 1` works best when each entry uses the same `Volume` size.

## Differences from the MT4 version
- Safety check for missing price step information with a log warning instead of silently using `Point`.
- Added guard clauses to ensure the strategy trades only when all required data (price history and both stochastic oscillators) is available.
- The strategy runs on finished candles only, while MT4 processed ticks but throttled by bar time. This change avoids duplicate evaluations and keeps the logic deterministic.
