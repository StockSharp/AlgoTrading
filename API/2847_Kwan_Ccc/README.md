# KWAN CCC Strategy

## Overview
The KWAN CCC strategy reproduces the MetaTrader expert `Exp_KWAN_CCC.mq5` using StockSharp's high level API. The system derives trading signals from a custom oscillator built as follows:

1. Calculate the Chaikin oscillator (difference between fast and slow moving averages of the accumulation/distribution line).
2. Multiply the Chaikin value by the Commodity Channel Index (CCI).
3. Divide the result by the Momentum indicator value. When momentum equals zero the script substitutes a constant value of 100 to avoid division by zero, exactly like the original code.
4. Smooth the resulting series with the user-selected XMA method.
5. Detect the slope of the smoothed series. Rising bars are coloured `0`, falling bars `2`, otherwise `1`.

When the colour changes from `0` to anything else the strategy closes shorts and opens a long position. When the colour changes from `2` to anything else it closes longs and opens a short. This mirrors the logic implemented in the MQL expert, including the optional signal shift (`SignalBar`).

## Trading Rules
- **Long entry**: colour on the bar at `SignalBar + 1` equals `0` and the bar at `SignalBar` is different from `0`.
- **Short entry**: colour on the bar at `SignalBar + 1` equals `2` and the bar at `SignalBar` is different from `2`.
- **Long exit**: enabled when `EnableLongExits = true` and the short entry condition triggers.
- **Short exit**: enabled when `EnableShortExits = true` and the long entry condition triggers.
- Protective stop and target orders are created through `StartProtection` using absolute price offsets derived from `StopLossPoints` and `TakeProfitPoints` multiplied by the instrument `PriceStep`.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `OrderVolume` | Base order size used when opening a new position. |
| `CandleType` | Timeframe for all indicator calculations. Default is 1 hour. |
| `FastPeriod` / `SlowPeriod` | Lengths of the moving averages inside the Chaikin oscillator. |
| `ChaikinMethod` | Moving average type (simple, exponential, smoothed, weighted) applied to the accumulation/distribution line. |
| `CciPeriod` | Period of the Commodity Channel Index. |
| `MomentumPeriod` | Period of the Momentum indicator. |
| `SmoothingMethod` | XMA smoothing method mapped from the original options. `JurX`, `Parabolic`, and `T3` fall back to Jurik MA; `Vidya` uses a Chande Momentum Oscillator driven adaptive smoothing; `Adaptive` uses Kaufman AMA. |
| `SmoothingLength` | Number of bars used by the selected smoothing filter. |
| `SmoothingPhase` | Additional parameter used by specific methods (e.g., VIDYA CMO length, AMA slow period). |
| `SignalBar` | Offset (in completed bars) used to evaluate the colour transitions. `1` reproduces the MetaTrader default. |
| `EnableLongEntries` / `EnableShortEntries` | Allow or block opening new positions in the corresponding direction. |
| `EnableLongExits` / `EnableShortExits` | Allow or block indicator-driven position closing. |
| `StopLossPoints` / `TakeProfitPoints` | Protective stop/target measured in price steps (set to zero to disable). |

## Implementation Notes
- The strategy only acts on finished candles and uses StockSharp's `Bind` helper to stream candle data into the indicators.
- The smoothing method list mirrors the XMA implementation from the original library. Methods that are unavailable in StockSharp are mapped to the closest alternative, as noted in the parameter table.
- MetaTrader's `VolumeType` input is omitted because StockSharp candles already encapsulate total volume information used by the accumulation/distribution line.
- Money management in the original expert relied on custom lot sizing helpers. The conversion assumes a fixed volume specified by `OrderVolume`.

## Usage Tips
- Ensure that the instrument provides meaningful volume data if Chaikin oscillator behaviour is important. For illiquid instruments consider increasing `MomentumPeriod` to reduce noise.
- When optimising smoothing parameters, combine `SmoothingLength` and `SmoothingPhase` carefully: extreme combinations may delay signals considerably.
- The default protective values (`StopLossPoints = 1000`, `TakeProfitPoints = 2000`) correspond to large offsets. Adjust them to match the instrument tick size.
