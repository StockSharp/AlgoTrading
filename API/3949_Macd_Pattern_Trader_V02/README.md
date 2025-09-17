# Macd Pattern Trader v02 (StockSharp Port)

This strategy is a StockSharp high-level API conversion of the MetaTrader expert **MacdPatternTraderv02.mq4** (directory `MQL/8194`). It reproduces the original MACD pattern detection and the active position management rules while exposing convenient parameters for further optimization.

## Core Idea

1. Calculate the MACD main line using the fast and slow EMA periods (`FastEmaPeriod`, `SlowEmaPeriod`) with a signal length of one candle (matching the MQL version).
2. Monitor completed candles only. When the MACD value paints a specific three-bar sequence around the zero line, arm either the short or the long pattern:
   - **Short pattern**: requires a positive MACD phase followed by a negative pullback above `MinThreshold` and then a downward inflection.
   - **Long pattern**: requires a negative MACD phase followed by a positive pullback below `MaxThreshold` and then an upward inflection.
3. Execute market orders using `TradeVolume` once the pattern confirms.
4. Protect each position with a stop-loss placed beyond the recent swing extreme (over `StopLossBars` candles) plus an additional offset in points (`OffsetPoints`).
5. Define the take-profit level by scanning consecutive `TakeProfitBars` segments and picking the most extreme high/low reached while the sequence keeps printing new extremes.
6. Manage open positions with the active position manager from the original expert: after a minimum profit of five points is achieved, the strategy closes one-third of the volume when the previous candle confirms the trend (`Ema2Period` filter) and another half when price interacts with the midline of `SmaPeriod` and `Ema3Period`.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `StopLossBars` | Number of completed candles inspected when calculating the stop-loss swing extreme. |
| `TakeProfitBars` | Window size (in candles) for the sequential extremum search that builds the take-profit target. |
| `OffsetPoints` | Additional offset, expressed in instrument points, added to the stop-loss. |
| `FastEmaPeriod` | Fast EMA length for the MACD main line. |
| `SlowEmaPeriod` | Slow EMA length for the MACD main line. |
| `MaxThreshold` | Positive MACD threshold that terminates the short pattern preparation. |
| `MinThreshold` | Negative MACD threshold that terminates the long pattern preparation. |
| `Ema1Period` | First EMA period used by the original money-management block (kept for completeness). |
| `Ema2Period` | Second EMA period used to validate partial profit for long/short positions. |
| `SmaPeriod` | SMA period used in the second partial close trigger. |
| `Ema3Period` | Slow EMA period paired with the SMA to detect mean-reversion exits. |
| `TradeVolume` | Market order volume (lots). |
| `CandleType` | Candle data type used to feed all indicators. |

## Trading Logic

- **Short entry**: triggered when the MACD sequence `(prev3, prev2, prev1, current)` matches the original conditions (`macdPrev1 < macdPrev3`, `macdPrev1 > macdPrev2`, `current < prev1`, `current < 0`, and magnitude check). Existing long exposure is flattened before opening a new short position.
- **Long entry**: symmetrical rules where `current > 0`, the MACD values form the mirror image pattern, and the magnitude check is satisfied. Existing short exposure is flattened before opening a new long position.
- **Stops and targets**: computed immediately after each entry and updated only when a fresh trade is executed.
- **Partial closes**: once profit reaches five points (relative to the instrument point size), the strategy closes one-third of the remaining volume if the previous candle closes beyond `EMA2`. The next stage closes half of the remaining volume when the previous candle pierces the average of `SMA` and `EMA3`.
- **Full exit**: any price touch of the stop-loss or take-profit level closes the entire position. After each forced exit the internal state resets automatically.

## Notes

- The point size is derived from `Security.PriceStep` or, when unavailable, from the security decimals. A default value of `0.0001` is used as a safe fallback.
- Candle history is stored (up to 1024 entries) to replicate the MQL helper functions `iHighest`, `iLowest`, and the sequential extremum scan from `TakeProfit()`.
- All comments inside the strategy remain in English, as required by the repository guidelines.
- Python ports are intentionally omitted for this task.
