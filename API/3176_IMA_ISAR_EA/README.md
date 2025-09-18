# iMA iSAR EA Strategy

## Overview
This strategy replicates the "iMA iSAR EA" Expert Advisor from MetaTrader 5 using StockSharp's high-level API. It combines a triple weighted moving average filter with two Parabolic SAR trails to identify momentum breakouts. A long position is opened when the fastest weighted moving average stays above the other two averages and both SAR trails sit below the candle close. A mirrored condition generates short entries. Protective stops, profit targets, and an optional trailing stop are managed in price points (pips).

The implementation works on a single candle series that is configurable through the `CandleType` parameter. All indicators are evaluated on this timeframe. The original MetaTrader expert used multiple timeframes for its indicators; in StockSharp this behaviour is approximated by allowing individual moving-average shifts that can delay each signal by a number of completed bars.

## Trading Rules
- **Indicators**
  - Three weighted moving averages (`Fast`, `Normal`, `Slow`) calculated on the configured candle stream. Optional bar shifts emulate the delayed buffers from the original MQ5 code.
  - Two Parabolic SAR indicators (`FastSAR`, `NormalSAR`) share the same candle stream but have independent acceleration and maximum step parameters.
- **Entry Conditions**
  - **Long**: `Fast` MA is above `Normal` and `Slow`, while both SAR values are below the candle close.
  - **Short**: `Fast` MA is below `Normal` and `Slow`, while both SAR values are above the candle close.
  - When a reversal signal appears the strategy closes any opposite exposure and flips direction in a single market order.
- **Risk Controls**
  - Fixed stop-loss and take-profit levels are expressed in pips (multiples of the security price step). They are evaluated against completed candles.
  - Optional trailing stop: once enabled the stop follows the close price at a configurable distance and only advances after moving by the specified trailing step.
  - Volumes are adjusted to the security's `VolumeStep`, `MinVolume`, and `MaxVolume` settings before sending orders.

## Parameters
| Name | Type | Default | Description |
|------|------|---------|-------------|
| `Volume` | `decimal` | `0.1` | Base order size. Automatically increased to cover an opposite position when flipping direction. |
| `StopLossPips` | `decimal` | `50` | Protective stop distance in pips. Set to `0` to disable. |
| `TakeProfitPips` | `decimal` | `50` | Profit target distance in pips. Set to `0` to disable. |
| `UseTrailing` | `bool` | `true` | Enables dynamic trailing stop management. |
| `TrailingStopPips` | `decimal` | `25` | Distance between price and trailing stop, in pips. |
| `TrailingStepPips` | `decimal` | `5` | Minimum favourable movement (pips) before the trailing stop is advanced. |
| `CandleType` | `DataType` | `TimeFrameCandle 15m` | Candle series used for all calculations. |
| `FastMaPeriod` | `int` | `10` | Period of the fast weighted moving average. |
| `FastMaShift` | `int` | `0` | Number of completed bars to shift the fast MA backwards. |
| `NormalMaPeriod` | `int` | `30` | Period of the normal weighted moving average. |
| `NormalMaShift` | `int` | `3` | Number of completed bars to shift the normal MA backwards. |
| `SlowMaPeriod` | `int` | `60` | Period of the slow weighted moving average. |
| `SlowMaShift` | `int` | `6` | Number of completed bars to shift the slow MA backwards. |
| `FastSarStep` | `decimal` | `0.02` | Acceleration factor for the fast Parabolic SAR. |
| `FastSarMax` | `decimal` | `0.2` | Maximum acceleration for the fast Parabolic SAR. |
| `NormalSarStep` | `decimal` | `0.02` | Acceleration factor for the normal Parabolic SAR. |
| `NormalSarMax` | `decimal` | `0.2` | Maximum acceleration for the normal Parabolic SAR. |

## Notes
- Trailing stop checks are performed on candle close. If intrabar precision is required, combine the strategy with a tick-level protective component.
- The pip size equals the security price step when it is available. Otherwise a standard `0.0001` tick is assumed for FX pairs.
- For consistency with the MetaTrader version, all indicator signals operate on closed candles. Pending transactions are not staged; each signal submits an immediate market order.
