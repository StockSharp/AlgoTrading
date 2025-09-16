# Color Schaff JJRSX MMRec Duplex Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor `Exp_ColorSchaffJJRSXTrendCycle_MMRec_Duplex`. The original robot combines two Schaff Trend Cycle oscillators driven by JJRSX momentum and an MMRec (Money Management Recalculation) module that reduces exposure after a sequence of losses. The C# conversion preserves the dual long/short layout and mirrors the adjustable risk controls while replacing the unavailable JJRSX indicator with a robust in-platform approximation.

## Trading Logic
- Two independent oscillators are calculated on user-selected time-frames: one governs long entries, the other governs short entries. Each oscillator uses fast and slow RSX-style momentum lines, smoothed and normalised with a Schaff Trend Cycle pipeline to output values in the range [-100, 100].
- A long position is opened when the long oscillator crosses down through zero (`previous > 0` and `current <= 0`). The original expert marks these events as bullish momentum reversals. Long exits trigger whenever the indicator value one bar earlier is negative.
- A short position is opened when the short oscillator crosses up through zero (`previous < 0` and `current >= 0`). Short exits trigger whenever the indicator value one bar earlier is positive.
- The `SignalBar` setting reproduces the MetaTrader behaviour of evaluating signals on historical bars. For example, `SignalBar = 1` inspects the last fully closed candle and the candle before it. The strategy maintains rolling indicator histories to emulate the `CopyBuffer` calls from the MQL code.

## Money Management (MMRec)
- Separate money management blocks are maintained for long and short trades. The base volume equals `Strategy.Volume * MM`, where `MM` is the configurable normal multiplier (`LongMm`/`ShortMm`).
- After every closed trade the strategy records whether the result was profitable or not (based on the entry/exit candle prices, identical to the EA logic that tracks history via `HistorySelect`).
- If the latest `TotalTrigger` trades contain at least `LossTrigger` losers, the next order for that side switches to the reduced multiplier (`SmallMm`). When the loss condition disappears the base multiplier is restored automatically.
- Position reversals respect the MMRec rules: flipping from long to short (or vice versa) first finalises the existing trade’s result and updates the loss counters before sizing the new order.

## Indicator Approximation
The original robot relies on a bespoke `ColorSchaffJJRSXTrendCycle` indicator built on the JJRSX oscillator and Jurik smoothing libraries. StockSharp does not ship those components, so the conversion implements `ColorSchaffJjrsxTrendCycleIndicator`:
- A lightweight RSI approximation (`SimpleRsi`) computes the momentum baseline with exponential smoothing identical to the EA’s smoothing period.
- Fast and slow RSI curves are subtracted to obtain a MACD-like series which is then normalised across a cyclical window and double-smoothed with a configurable factor (default 0.5) to mimic the Schaff Trend Cycle behaviour.
- The indicator accepts the same price sources (close, open, high, low, median, typical, weighted, etc.) and retains the cycle/length parameters so optimisation workflows remain faithful to the source strategy.

## Parameters
| Group | Name | Description |
| --- | --- | --- |
| Long | `LongCandleType` | Candle type or time-frame used for the long indicator. |
| Long | `LongTotalTrigger` | Number of completed long trades inspected when evaluating the loss counter. |
| Long | `LongLossTrigger` | Minimum number of losses within the inspected window that activates the reduced multiplier. |
| Long | `LongSmallMm` | Reduced volume multiplier applied after repeated losses. |
| Long | `LongMm` | Default long volume multiplier. |
| Long | `LongEnableOpen` | Enables long entries. |
| Long | `LongEnableClose` | Enables long exits. |
| Long | `LongFastLength` | Fast JJRSX period approximation. |
| Long | `LongSlowLength` | Slow JJRSX period approximation. |
| Long | `LongSmooth` | Exponential smoothing length applied before Schaff normalisation. |
| Long | `LongCycleLength` | Cycle window used for min/max normalisation. |
| Long | `LongSignalBar` | Historical shift used when analysing long signals. |
| Long | `LongAppliedPrice` | Price source used by the long indicator. |
| Short | `ShortCandleType` | Candle type or time-frame used for the short indicator. |
| Short | `ShortTotalTrigger` | Number of completed short trades inspected when evaluating the loss counter. |
| Short | `ShortLossTrigger` | Minimum number of losses within the inspected window that activates the reduced multiplier. |
| Short | `ShortSmallMm` | Reduced volume multiplier applied after repeated losses. |
| Short | `ShortMm` | Default short volume multiplier. |
| Short | `ShortEnableOpen` | Enables short entries. |
| Short | `ShortEnableClose` | Enables short exits. |
| Short | `ShortFastLength` | Fast JJRSX period approximation for shorts. |
| Short | `ShortSlowLength` | Slow JJRSX period approximation for shorts. |
| Short | `ShortSmooth` | Exponential smoothing length applied before Schaff normalisation for shorts. |
| Short | `ShortCycleLength` | Cycle window used for min/max normalisation on the short side. |
| Short | `ShortSignalBar` | Historical shift used when analysing short signals. |
| Short | `ShortAppliedPrice` | Price source used by the short indicator. |

## Implementation Notes
- The strategy uses StockSharp’s high-level candle subscriptions and avoids direct access to indicator buffers, matching the conversion guidelines.
- Protective stops (`StopLoss`/`TakeProfit`) from the MQL version are not ported because MetaTrader uses point-based distances; users can attach `StartProtection` or custom risk modules if needed.
- Trade history is evaluated using candle close prices, which mirrors the EA’s reliance on historical deal records while keeping the logic deterministic inside StockSharp.
- The custom indicator exposes `IsFormed` so the strategy only reacts once enough data has accumulated, preventing premature signals during warm-up.

## Disclaimer
This port replicates the logical structure of the MetaTrader strategy, but performance may differ because of data feeds, execution policies, and the JJRSX approximation. Always validate the behaviour on demo data before deploying it live.
