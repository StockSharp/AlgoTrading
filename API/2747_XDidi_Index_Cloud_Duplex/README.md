# XDidi Index Cloud Duplex Strategy

## Overview
The XDidi Index Cloud Duplex strategy replicates the dual long/short signalling logic of the original MQL5 expert *Exp_XDidi_Index_Cloud_Duplex*. Two independent XDidi index configurations are evaluated on configurable timeframes. Each configuration computes a ratio between fast/medium and slow/medium moving averages. Crossings between these ratios trigger market entries while persistent divergences trigger exits.

## Trading Logic
1. **Indicator calculation**
   - Three moving averages are calculated for each block (fast, medium, slow) on a selected price source.
   - The XDidi ratios are derived as `fast / medium` and `slow / medium`. Optional inversion matches the original `Revers` option.
2. **Signal generation**
   - Long block: when the previous bar had `fast > slow` and the signal bar closes with `fast <= slow`, a long entry is requested. If the previous bar had `fast < slow`, a long exit is requested.
   - Short block: when the previous bar had `fast < slow` and the signal bar closes with `fast >= slow`, a short entry is requested. If the previous bar had `fast > slow`, a short exit is requested.
   - Signal bar offsets reproduce the original `SignalBar` inputs.
3. **Order management**
   - Entries are executed with the strategy volume. Opposite positions are closed before reversing.
   - Optional stop-loss and take-profit levels are applied via `StartProtection` using price-step distances.

## Parameters
| Name | Description |
| --- | --- |
| `LongCandleType`, `ShortCandleType` | Candle timeframes for each block. |
| `LongFastMethod` / `Medium` / `Slow` & `ShortFastMethod` / `Medium` / `Slow` | Moving-average smoothing methods for fast, medium and slow curves. Unsupported legacy smoothers fall back to exponential averaging. |
| `LongFastLength`, `LongMediumLength`, `LongSlowLength` | Periods for the long block moving averages. |
| `ShortFastLength`, `ShortMediumLength`, `ShortSlowLength` | Periods for the short block moving averages. |
| `LongAppliedPrice`, `ShortAppliedPrice` | Price source used for each block (close, open, typical, Demark, etc.). |
| `EnableLongEntries`, `EnableShortEntries` | Toggle new long/short positions. |
| `EnableLongExits`, `EnableShortExits` | Toggle automatic exits. |
| `LongSignalBar`, `ShortSignalBar` | Historical shift (bars back) evaluated for crossings. |
| `LongReverse`, `ShortReverse` | Invert ratios (mirrors `Revers` flag in MQL). |
| `StopLossPoints`, `TakeProfitPoints` | Protective distances expressed in price steps (set to zero to disable). |
| `Volume` (base strategy property) | Defines default trade size. |

## Implementation Notes
- Moving averages are taken from StockSharp's indicator library. Advanced smoothers (`JJMA`, `JurX`, `ParMA`, `VIDYA`) default to exponential smoothing because direct equivalents are unavailable.
- Indicator values are processed on finished candles only, matching the original `IsNewBar` behaviour.
- Signal queues maintain only the required number of historical ratio values, avoiding heavy collections.
- Protective stops are optional; if both distances are zero the strategy still calls `StartProtection()` to comply with the framework lifecycle.

## Usage Tips
- Align candle types with the data subscription available in your connector.
- Optimise moving-average lengths and applied prices to fit the traded instrument.
- When using asymmetric timeframes (long/short), both subscriptions are visualised on separate chart areas for clarity.

## Limitations Compared to MQL5 Version
- Money-management modes (`MM`, `MarginMode`) are not replicated; trade size follows the StockSharp `Volume` property.
- Some exotic smoothing algorithms from `SmoothAlgorithms.mqh` are approximated with exponential moving averages.
- Stop/limit orders are converted to generic protection levels instead of individual order parameters.
