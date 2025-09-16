# Cyberia Trader AI Strategy

This strategy is a StockSharp conversion of the **CyberiaTrader.mq4 (build 8553)** expert advisor. The original MQL program mixes a
probability engine with a collection of optional trend filters. The C# port keeps the same structure: a probability model searches
for the most reliable sampling period and then optional MACD, EMA and reversal filters can veto trades.

## Indicators and Internal Model

- **Probability Engine** – iterates candidate sampling periods (`MaxPeriod`) and evaluates `SamplesPerPeriod` historical segments.
  For each period the engine calculates:
  - Decision direction (buy/sell/flat) based on consecutive bullish/bearish one-minute candles spaced by the sampling period.
  - Average "possibility" amplitudes for buy, sell and undefined outcomes and the share of successful outcomes above
    `SpreadThreshold`.
  - Success ratios that select the best performing period.
- **EMA Trend Filter** – optional exponential moving average (`EnableMa`) that blocks trades against the current slope.
- **MACD Filter** – optional moving average convergence/divergence (`EnableMacd`) that forbids trading against momentum.
- **Reversal Detector** – optional spike detector (`EnableReversalDetector`) that flips permissions when probabilities jump above
  `ReversalFactor` multiples of their averages.

## Parameters

| Name | Description |
| --- | --- |
| `MaxPeriod` | Largest sampling stride inspected by the probability engine. |
| `SamplesPerPeriod` | Number of segments processed per period candidate (mirrors the MQL `ValuesPeriodCount`). |
| `SpreadThreshold` | Minimal amplitude that counts as a successful probability outcome. |
| `EnableCyberiaLogic` | Enables the Cyberia probability switches that can disable buys or sells. |
| `EnableMacd` | Enables the MACD momentum filter. |
| `EnableMa` | Enables the EMA slope filter. |
| `EnableReversalDetector` | Enables the reversal detector toggling permissions on extreme spikes. |
| `MaPeriod` | EMA length used by the trend filter. |
| `MacdFast` / `MacdSlow` / `MacdSignal` | MACD fast EMA, slow EMA, and signal periods. |
| `ReversalFactor` | Multiplier that triggers the reversal detector. |
| `CandleType` | Candle data type processed by the model (default 1 minute). |
| `TakeProfitPercent` | Optional protective take profit expressed as a percent. |
| `StopLossPercent` | Optional protective stop loss expressed as a percent. |

## Trading Logic

1. Each completed candle updates the local history queue and recomputes probability statistics for every period from 1 to
   `MaxPeriod`. The period with the highest success ratio becomes the active configuration.
2. The Cyberia logic sets `DisableBuy`/`DisableSell` flags using the same comparisons as the MQL code:
   - Compares buy/sell average possibilities and their success-weighted variants when the period increases or decreases.
   - Disables entries if fresh possibilities exceed twice their successful averages.
3. Optional filters are applied in order: MACD, EMA slope, then the reversal detector.
4. When no position is open, the strategy enters if the current decision is buy (or sell) and the corresponding possibility exceeds
   its successful average while the opposite direction is disabled.
5. While a position exists the code checks the same conditions to close when the probability engine flips or when filters forbid the
   current direction.
6. `StartProtection` reproduces the original money management blocks when non-zero risk parameters are supplied.

## Notes on the Conversion

- The port keeps the statistical calculations but replaces the tick-based spread check with the configurable `SpreadThreshold`.
- Auto lot sizing and balance diagnostics from the MQL script are not implemented; StockSharp volume is controlled via `Volume`.
- MoneyTrain and Pipsator modules are condensed into the unified entry/exit logic described above to match high-level API usage.
- The strategy adds chart drawing for candles, EMA and MACD to ease validation in the designer.
