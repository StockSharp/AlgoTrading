# XDeMarker Histogram Vol Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy reproduces the original MetaTrader expert advisor **Exp_XDeMarker_Histogram_Vol** on top of the StockSharp high-level API. It transforms the DeMarker oscillator into a volume-weighted histogram, smooths both the oscillator and volume with configurable moving averages, and reacts to regime changes when the histogram crosses predefined bands.

The logic is deliberately symmetrical. Long positions are opened when the histogram steps into one of the bullish zones, while shorts are opened when it moves into bearish zones. Opposite signals close the active position and, if enabled, immediately flip the direction.

## Concept

1. **Volume-weighted DeMarker**
   - DeMarker is calculated with the selected period.
   - The oscillator is scaled to the `[-50; +50]` range and multiplied by the chosen candle volume.
   - A moving average smooths the weighted oscillator. The same moving average is applied to the volume itself. Only four moving-average types are provided (simple, exponential, smoothed, weighted) because these are natively available in StockSharp.
2. **Dynamic levels**
   - Four user-defined multipliers (`HighLevel1`, `HighLevel2`, `LowLevel1`, `LowLevel2`) define the bullish and bearish thresholds.
   - Thresholds are scaled by the smoothed volume so that higher participation widens the acceptable range.
3. **State machine**
   - Each finished candle is classified into one of five states: `0` (extreme bullish), `1` (bullish), `2` (neutral), `3` (bearish), `4` (extreme bearish).
   - Signals are generated when the state of the last closed candle (offset by `SignalBar`) differs from the prior state in a way that indicates a transition into bullish or bearish territory.

## Parameters

| Name | Description |
| --- | --- |
| `CandleType` | Primary timeframe. Defaults to 2-hour candles to mirror the original expert advisor. |
| `DeMarkerPeriod` | Period of the DeMarker oscillator. |
| `HighLevel1` / `HighLevel2` | Positive multipliers defining the first and second bullish thresholds. |
| `LowLevel1` / `LowLevel2` | Negative multipliers defining the first and second bearish thresholds. |
| `Smoothing` | Moving average type for both the histogram and the volume. Choices: Simple, Exponential, Smoothed, Weighted. |
| `SmoothingLength` | Length of the smoothing averages. |
| `SignalBar` | Number of closed bars used for signal comparison. `1` means “use the most recently closed candle”. |
| `VolumeType` | Volume source. Both options fall back to candle volume because StockSharp does not expose tick counts on all feeds. |
| `EnableLongEntries` / `EnableShortEntries` | Allow opening new positions in the respective direction. |
| `EnableLongExits` / `EnableShortExits` | Allow closing existing positions when the opposite setup appears. |

## Signals and Position Management

- **Enter Long**: the latest signal bar transitions into state `1` or `0` while the previous bar was in a higher-numbered state (>1). Short positions are optionally closed before entering.
- **Enter Short**: the latest signal bar transitions into state `3` or `4` while the previous bar was in a lower-numbered state (<3 or <4 respectively). Long positions are optionally closed before entering.
- **Exit**: whenever an opposite signal is triggered and exits are enabled for the current direction. `ClosePosition()` is used to flatten before reversing.
- **Position Sizing**: the strategy relies on the standard `Strategy.Volume` property. The money-management blocks from the MetaTrader version (two separate “magic” IDs) are intentionally simplified.

## Implementation Notes

- Only finished candles are processed. The strategy subscribes to the configured timeframe via `SubscribeCandles().WhenNew(ProcessCandle)`.
- The DeMarker implementation keeps rolling sums of DeMax/DeMin values to match MetaTrader calculations and waits until enough bars are accumulated before emitting signals.
- If volume data is missing, the histogram degrades gracefully to zero because both the weighted oscillator and thresholds will be zero.
- Unsupported smoothing modes from the original indicator (JJMA, JurX, ParMA, T3, VIDYA, AMA) are not reproduced. Choose the closest alternative via the `Smoothing` parameter.
- The `SignalBar` buffer keeps only the minimum history needed (current, previous, and one extra slot) to mimic the original `CopyBuffer` behaviour and avoid stale signals.

## Usage Tips

- Start the strategy in the Designer or Runner after configuring the desired timeframe and volume.
- Optimise `DeMarkerPeriod`, `SmoothingLength`, and the threshold multipliers together—small changes in thresholds materially alter the entry cadence.
- Because the histogram is volume-weighted, feed quality matters. Use data providers that report reliable candle volume to capture the intended effect.
- Consider adding external money-management or risk modules if you need stop-loss or take-profit rules; they were not present in the high-level conversion.
