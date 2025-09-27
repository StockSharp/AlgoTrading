# FiftyFiveMaBarComparisonStrategy

## Overview
The strategy replicates the MetaTrader 5 "55 MA" expert advisor by comparing two points of a 55-period moving average and trading whenever their difference exceeds a configurable threshold. All calculations are performed on completed candles within a user-defined intraday session, and trade direction can optionally be inverted. The algorithm preserves the original behaviour where a short position is opened whenever no bullish condition is met.

## Trading Logic
1. Subscribe to the selected candle series and calculate a moving average with the chosen length, method and applied price.
2. Maintain the most recent moving-average values in a buffer so that the values at bar indexes `BarA` and `BarB` can be accessed even when a horizontal MA shift is used.
3. When a finished candle arrives inside the `[StartHour, EndHour)` window:
   - Retrieve the MA value at `BarA + MaShift` and `BarB + MaShift`.
   - If the value at `BarA` exceeds the value at `BarB` by more than `DifferenceThreshold`, open a long position unless `ReverseSignals` is enabled.
   - If the value at `BarA` is lower than the value at `BarB` by more than `DifferenceThreshold`, open a short position (or a long position when `ReverseSignals` is enabled).
   - Otherwise the strategy keeps the original EA behaviour and triggers a short entry.
4. Orders are always sent at the market using the strategy `Volume`. When `CloseOppositePositions` is enabled the requested size is increased to flatten any opposite exposure before establishing the new position.
5. Optional stop-loss and take-profit protections are attached through `StartProtection`. Distances are expressed in pips, where one pip equals `PriceStep` multiplied by 10 for instruments quoted with 3 or 5 decimal digits.

## Inputs
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-minute time frame | Candle series used for calculations and signals. |
| `StopLossPips` | `int` | 30 | Stop-loss distance in pips. Set to 0 to disable. |
| `TakeProfitPips` | `int` | 50 | Take-profit distance in pips. Set to 0 to disable. |
| `StartHour` | `int` | 8 | Inclusive hour (0-23) that marks the start of the trading session. |
| `EndHour` | `int` | 21 | Exclusive hour (0-23) that marks the end of the trading session. Must be greater than `StartHour`. |
| `DifferenceThreshold` | `decimal` | 0.0001 | Minimal absolute difference between the compared MA values that triggers a directional signal. |
| `BarA` | `int` | 0 | Index of the first bar used for the MA comparison (0 = current candle). |
| `BarB` | `int` | 1 | Index of the second bar used for the MA comparison. |
| `ReverseSignals` | `bool` | `false` | Inverts the bullish and bearish conditions. |
| `CloseOppositePositions` | `bool` | `false` | If enabled, increases the order size to close any position in the opposite direction before opening the new trade. |
| `MaShift` | `int` | 0 | Horizontal shift applied to the moving average line. Positive values access older MA points. |
| `MaLength` | `int` | 55 | Period of the moving average. |
| `MaMethod` | `MovingAverageMethods` | `Exponential` | Smoothing method (`Simple`, `Exponential`, `Smoothed`, `Weighted`). |
| `AppliedPrice` | `AppliedPriceTypes` | `Median` | Price used as MA input (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |

## Position Management
- Set the strategy `Volume` to control the base trade size. It is combined with the current position when `CloseOppositePositions` is active.
- Stop-loss and take-profit protections are optional. They are attached only when the respective pip distance is greater than zero.

## Notes
- The trading window works in instrument time; signals outside `[StartHour, EndHour)` are skipped.
- When `MaShift` produces negative indexes the strategy waits until enough history accumulates, mirroring the original EA behaviour where shifted buffers can return `EMPTY_VALUE`.
- Because the original expert always defaults to a sell order when the difference threshold is not met, the converted strategy keeps the same logic for full fidelity. Adjust `DifferenceThreshold` if this behaviour is undesirable.
