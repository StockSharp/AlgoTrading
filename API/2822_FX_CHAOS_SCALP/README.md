# FX-CHAOS Scalp Strategy

## Overview
The FX-CHAOS scalp strategy replicates the MT5 expert advisor that combines the Awesome Oscillator with fractal-based ZigZag levels on multiple timeframes. The StockSharp port subscribes to hourly candles for trade execution and daily candles for a higher timeframe filter. Internal trackers rebuild the "ZigZag on Fractals" logic by detecting five-candle fractal patterns and stitching them into alternating swing points.

## Trading Workflow
1. **Data collection**
   - Hourly candles drive entries and risk management.
   - Daily candles feed the higher timeframe ZigZag filter.
   - An Awesome Oscillator (5, 34) is calculated on the hourly feed.
2. **Fractal ZigZag tracking**
   - Each finished candle is fed into a five-element sliding window.
   - When the middle bar forms an up/down fractal, the latest swing value is updated; consecutive swings in the same direction are only replaced by more extreme values.
3. **Signal detection on hourly close**
   - A long signal appears when the candle opens below the previous high, closes above it, stays below the most recent hourly ZigZag swing, remains above the latest daily ZigZag level, and the Awesome Oscillator is negative.
   - A short signal mirrors the logic using the previous low and opposite oscillator polarity.
4. **Order execution**
   - Existing opposite positions are flattened before a new entry is placed with the configured volume.
   - The entry price is stored for subsequent stop-loss and take-profit management.

## Parameters
| Name | Description |
| --- | --- |
| `Volume` | Trade volume in lots. Applied to every market order. |
| `Stop Loss (pts)` | Distance in points for the protective stop. The value is multiplied by the security price step. Set to `0` to disable. |
| `Take Profit (pts)` | Distance in points for the profit target. Converted with the price step in the same way. Set to `0` to disable. |
| `Trading Candle` | Primary timeframe used for entries (defaults to 1 hour). |
| `Daily Candle` | Higher timeframe used for the ZigZag filter (defaults to 1 day). |

## Risk Management
- On every finished hourly candle the strategy checks whether price touched the stop-loss or take-profit level derived from the stored entry price.
- A filled protective order closes the position immediately and resets the entry price flag, preventing a re-entry in the same candle cycle.
- Positions are also flattened whenever a new signal in the opposite direction appears.

## Implementation Notes
- The custom ZigZag logic avoids direct indicator buffers and follows the repository guidelines by working on candle subscriptions with minimal local state.
- ZigZag values remain `null` until enough candles are processed (two bars on each side of a potential fractal). Trading is suspended until both hourly and daily trackers produce valid swings.
- The Awesome Oscillator is requested via `BindEx`, ensuring the strategy uses only final indicator values when all inputs are ready.
- Price distances are scaled by `Security.PriceStep`. If the instrument lacks a step the strategy falls back to a one-point multiplier.

## Files
- `CS/FxChaosScalpStrategy.cs` – strategy implementation with the ZigZag tracker, Awesome Oscillator filter, and order logic.
- `README_cn.md` – documentation in Simplified Chinese.
- `README_ru.md` – documentation in Russian.
