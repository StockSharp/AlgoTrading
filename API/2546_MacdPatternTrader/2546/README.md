# MacdPatternTrader Strategy

## Overview
The **MacdPatternTrader** strategy is a faithful high-level conversion of the MQL5 expert advisor `MacdPatternTraderAll0.01`. It subscribes to the configured candle series and evaluates six independent MACD-based entry patterns on every finished bar. Each pattern owns its own fast/slow EMA pair, stop-loss look-back horizon and MACD thresholds so that signals from one pattern do not interfere with the others. Whenever a pattern fires, the strategy submits a market order sized by the current martingale volume and immediately calculates the protective price levels.

The strategy is designed to reproduce the rather involved behaviour of the original EA:

* A shared martingale money-management block doubles the working volume after a losing trade cycle and resets it after profitable exits.
* Stop-loss prices are taken from recent highs/lows plus an offset expressed in points. The selection logic scans multiple segments exactly like the `iHighest`/`iLowest` loops from MQL.
* Take-profit levels chain several candle segments so that the target extends when the trend keeps making new extremes in the same direction.
* Partial exits close one third and then half of the remaining volume whenever the floating profit exceeds five currency units and additional EMA/SMA filters confirm the direction.
* Optional intraday filtering restricts entries to a `(StartTime, StopTime)` window while still supervising open positions around the clock.

## Market Data and Indicators
* **Data type** – Configurable candle series (`CandleType` parameter). The default matches the original H1 timeframe.
* **Indicators** – Six instances of `MovingAverageConvergenceDivergenceSignal` plus four moving averages (`EMA`, `EMA`, `SMA`, `EMA`) dedicated to the partial exit filter. All indicators are bound to the candle subscription via `BindEx`, so every call to the processing handler receives synchronized values.
* **History buffers** – A compact rolling candle window is retained to evaluate stop-loss and take-profit segments. MACD values are also cached to mirror the stage-based logic from the MQL implementation.

## Entry Logic
Every pattern runs independently and can open trades even if another position is already active. The EA therefore behaves like a signal aggregator.

1. **Pattern 1 – Threshold Reversal**
   * Waits for the MACD main line to cross above `Pattern1MaxThreshold` and then rotate downward while remaining positive to trigger a short setup.
   * Symmetrically, looks for dips below `Pattern1MinThreshold` followed by a rebound for long entries.
2. **Pattern 2 – Zero-Line Bounce**
   * Tracks whether the MACD previously stayed above zero. Once it crosses the neutral line and hooks downward, a short trade is armed.
   * After negative phases it arms a long setup that requires a recovery above zero combined with a higher close than the previous two MACD bars.
3. **Pattern 3 – Tiered Crest/Trough**
   * Recreates the multi-stage counters (`S3`, `stops3`, `stops13`, etc.) to detect complex crest structures for shorts and trough structures for longs.
   * Resets the auxiliary bar counter (`bars_bup`) after every executed order, keeping the behaviour identical to the MQL code.
4. **Pattern 4 – Local Extremes**
   * Identifies local MACD peaks or valleys when the current bar crosses back inside the thresholds while the previous value was an extreme compared with two bars ago.
5. **Pattern 5 – Neutral Band Breakout**
   * Uses neutral maximum/minimum thresholds to define a middle band. Long entries require a bounce from below the neutral band followed by a break above the bullish threshold. The short logic mirrors these steps.
6. **Pattern 6 – Consecutive Counter**
   * Counts how many consecutive bars remain above/below the configured thresholds. Trades only occur when the counter is between `Pattern6TriggerBars` and `Pattern6MaxBars` (for longs) or `Pattern6MinBars` (for shorts).

## Exit and Risk Management
* **Stop-loss** – For long trades the lowest low within `StopLossBars` plus the `Offset` (converted into price steps) becomes the protective level. Shorts mirror this logic with the highest high. The algorithm keeps scanning multiple `TakeProfitBars` segments just like the EA.
* **Take-profit** – Searches consecutive segments made of `TakeProfitBars` candles. When a new segment yields a more favourable extreme, the target extends to cover the new value; otherwise the loop stops.
* **Partial exits** – When the floating profit surpasses five currency units the strategy closes one third of the position (never dropping below `0.01`). A second threshold closes half of what remains. EMA/SMA filters ensure the exit respects the original management rules.
* **Martingale volume** – `InitialVolume` defines the base lot size. A losing cycle doubles `_currentVolume` if `UseMartingale` is true; profitable cycles reset it. The counters `_longPartialCount` and `_shortPartialCount` mirror the number of partial exits to prevent duplicate actions.
* **Time window** – When `UseTimeFilter` is enabled, entries are evaluated only if the candle closing time lies strictly between `StartTime` and `StopTime`. Risk management continues to work outside this window to keep stops active.

## Parameters
| Group | Name | Description |
| --- | --- | --- |
| Pattern 1 | `Pattern1Enabled` | Toggle pattern #1 detection. |
| Pattern 1 | `Pattern1StopLossBars`, `Pattern1TakeProfitBars`, `Pattern1Offset` | Stop-loss and take-profit look-back windows plus offset. |
| Pattern 1 | `Pattern1Slow`, `Pattern1Fast` | Slow and fast EMA lengths for the MACD instance. |
| Pattern 1 | `Pattern1MaxThreshold`, `Pattern1MinThreshold` | Upper/lower MACD thresholds that arm the signals. |
| Pattern 2 | Same parameter structure as pattern #1 with independent default values. |
| Pattern 3 | Adds secondary thresholds `Pattern3MaxLowThreshold`, `Pattern3MinHighThreshold` that steer the staged crest/trough logic. |
| Pattern 4 | Includes `Pattern4AdditionalBars` (kept for compatibility) plus additional max/min thresholds. |
| Pattern 5 | Uses neutral thresholds `Pattern5MaxNeutralThreshold` and `Pattern5MinNeutralThreshold` to define the middle band. |
| Pattern 6 | Introduces counters `Pattern6MaxBars`, `Pattern6MinBars`, `Pattern6TriggerBars` controlling the consecutive-bar logic. |
| Management | `EmaPeriod1`, `EmaPeriod2`, `SmaPeriod3`, `EmaPeriod4` | Moving averages for partial exit filters. |
| General | `InitialVolume`, `UseTimeFilter`, `StartTime`, `StopTime`, `UseMartingale`, `CandleType` | Global behaviour and data selection. |

## Implementation Notes
* The strategy uses only high-level StockSharp API calls (`SubscribeCandles`, `BindEx`, `BuyMarket`, `SellMarket`) and does not rely on any manual indicator value polling.
* Stops and targets are applied through `SetStopLoss`/`SetTakeProfit` and refreshed on every candle to follow the EA’s behaviour.
* Rolling collections are trimmed to 1,000 elements so that the logic matches the EA without growing unbounded in long sessions.
* Volume adjustments and realized PnL tracking are implemented inside `OnOwnTradeReceived`, ensuring martingale decisions are made immediately after every trade.
* The code adds descriptive English comments to clarify each logical block for further maintenance or auditing.

## Recommended Usage
1. Attach the strategy to an instrument that supports continuous trading (forex pairs, CFDs, or crypto).
2. Verify that the candle timeframe matches the original EA configuration (default is H1).
3. Adjust the pattern thresholds or disable specific patterns when optimizing for a new market.
4. Keep risk controls in mind: martingale scaling can grow exposure rapidly during prolonged losing streaks.
