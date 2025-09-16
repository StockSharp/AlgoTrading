# MacdPatternTrader Strategy

## Overview
The MacdPatternTrader strategy is a high level StockSharp conversion of the original *MacdPatternTraderAll* MQL expert advisor. The system listens to completed candles and evaluates six independent MACD based entry patterns. Every pattern uses its own fast and slow exponential moving averages plus dedicated threshold levels to recognize reversal and continuation structures on the MACD main line. Signals can arrive simultaneously and each one submits a market order sized by the current martingale volume.

The strategy supplements the entry logic with adaptive risk management. Stop-loss prices are computed from recent highs or lows with an offset, while take-profit targets extend through successive history blocks in the same manner as the MQL implementation. Open positions are actively managed by partial exits based on EMA/SMA filters and an unrealized profit threshold. After every flat close the martingale multiplier either resets or doubles the lot size depending on the realized result.

## Trading Rules
1. **Pattern 1 – Threshold reversal**
   * Tracks when the MACD main line rises above an upper threshold, then flips back below while staying positive.
   * Mirrors the behaviour for the lower threshold when the MACD recovers from negative territory.
2. **Pattern 2 – Zero level bounce**
   * Requires a positive MACD phase, then a bearish hook under the zero line before selling.
   * Uses the symmetric logic for bullish hooks above zero to buy.
3. **Pattern 3 – Multi stage sequence**
   * Reproduces the three stage crest and trough recognition from the MQL source using nested flags and threshold pairs.
   * Resets the auxiliary counters (`bars_bup`) after every executed trade.
4. **Pattern 4 – Local peak / valley**
   * Waits for MACD local highs or lows in relation to the previous two bars to set up short and long signals respectively.
5. **Pattern 5 – Neutral band breakout**
   * Looks for short entries after dipping below a neutral band and immediately returning underneath a bearish limit.
   * Looks for long entries after moving above the neutral band and jumping over a bullish limit.
6. **Pattern 6 – Consecutive bar counter**
   * Counts the number of bars above or below the configured thresholds and only triggers when the counter exceeds the `TriggerBars` value while staying below the `MaxBars` limit.

## Risk Management and Trade Management
* **Stop-loss** – Determined by the highest (for short trades) or lowest (for long trades) price over the last `StopLossBars` candles plus the configured offset translated into price step units.
* **Take-profit** – Searches consecutive history segments of `TakeProfitBars` candles, exactly like the nested `iLowest` / `iHighest` loops in the MQL version. The target extends while the next segment produces a more extreme value.
* **Partial exits** – Once the unrealized profit exceeds five currency units (approximated by price difference × position volume) and the EMA/SMA filters agree, the strategy closes one third of the open volume, then one half of the remainder.
* **Martingale lot control** – After a flat exit the strategy resets the lot to `InitialVolume` when the closed trade gained money; otherwise the volume doubles (if `UseMartingale` is enabled).
* **Time filter** – When `UseTimeFilter` is enabled the strategy only evaluates entries inside the `(StartTime, StopTime)` window. Stops are still checked on every finished candle.

## Parameters
| Group | Name | Description |
| --- | --- | --- |
| Pattern 1 | `Pattern1Enabled` | Enables the first MACD pattern. |
| Pattern 1 | `Pattern1StopLossBars`, `Pattern1TakeProfitBars`, `Pattern1Offset` | Stop-loss/take-profit lookback and offset settings. |
| Pattern 1 | `Pattern1Slow`, `Pattern1Fast` | Slow and fast EMA lengths for the MACD calculation. |
| Pattern 1 | `Pattern1MaxThreshold`, `Pattern1MinThreshold` | Upper and lower MACD thresholds. |
| Pattern 2 | Same structure as pattern 1 with its own values. |
| Pattern 3 | Adds extra thresholds `Pattern3MaxLowThreshold` and `Pattern3MinHighThreshold` to reproduce the tiered crest/trough recognition. |
| Pattern 4 | Includes `Pattern4AdditionalBars` (kept for compatibility with the original code). |
| Pattern 5 | Uses neutral thresholds for band breakout detection. |
| Pattern 6 | Adds `Pattern6MaxBars`, `Pattern6MinBars`, `Pattern6TriggerBars` to manage the bar counter logic. |
| Management | `EmaPeriod1`, `EmaPeriod2`, `SmaPeriod3`, `EmaPeriod4` | Moving averages for partial exit filters. |
| General | `InitialVolume`, `UseTimeFilter`, `StartTime`, `StopTime`, `UseMartingale`, `CandleType` | Global behaviour controls. |

## Notes
* The conversion keeps the original logic structure, including the segmented take-profit search and martingale reset rules.
* Profit based partial exits use an approximation because the StockSharp high level API does not expose raw terminal profit values per position; price difference × volume is used instead.
* `Pattern4AdditionalBars` is preserved for compatibility even though the original MQL code never referenced it directly.
* Stops and take profits are evaluated on closed candles because StockSharp does not attach protective orders automatically in the high level API.
