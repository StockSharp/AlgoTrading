# Blau Ergodic MDI Strategy

## Overview
The Blau Ergodic Market Directional Indicator (MDI) strategy reproduces the behaviour of the MetaTrader expert advisor `Exp_BlauErgodicMDI`. The algorithm operates on a higher timeframe candle stream (default 4H) and applies a triple smoothing pipeline to the selected price input in order to build a momentum histogram and a signal line. Trading decisions are derived from that histogram using one of three configurable entry modes:

1. **Breakdown** – trades when the histogram crosses the zero line.
2. **Twist** – reacts to reversals in the histogram slope (momentum changing direction).
3. **CloudTwist** – acts on histogram / signal-line crossovers.

Every signal can optionally close opposing positions and/or open new trades depending on the permission flags provided by the user.

## Indicator logic
1. Smooth the chosen applied price with the configured moving average type and `PrimaryLength` to obtain the baseline price.
2. Calculate the momentum difference `(price - baseline) / point_value`.
3. Smooth that momentum with `FirstSmoothingLength` and `SecondSmoothingLength` to build the histogram.
4. Smooth the histogram once more with `SignalLength` to obtain the signal line.
5. Buffer historical values according to `SignalBarShift` so that signals can be confirmed on closed candles.

Supported smoothing families are **EMA**, **SMA**, **SMMA/RMA**, and **WMA**. The applied price selection mirrors the MetaTrader implementation (close, open, high, low, median, typical, weighted, simple, quarter, trend-following variants).

## Parameters
| Name | Description |
| ---- | ----------- |
| `Volume` | Order size used when opening positions. |
| `StopLossPoints` | Stop loss distance in instrument points (0 disables). |
| `TakeProfitPoints` | Take profit distance in instrument points (0 disables). |
| `SlippagePoints` | Maximum price slippage in points applied to market orders. |
| `AllowLongEntries` / `AllowShortEntries` | Allow opening positions in the respective direction. |
| `AllowLongExits` / `AllowShortExits` | Allow closing existing positions on opposite signals. |
| `Mode` | Entry mode (Breakdown / Twist / CloudTwist). |
| `CandleType` | Timeframe of candles used for calculations (default 4H). |
| `SmoothingMethod` | Moving average family used in all smoothing steps. |
| `PrimaryLength` | Baseline smoothing length for the applied price. |
| `FirstSmoothingLength` | First smoothing length applied to momentum. |
| `SecondSmoothingLength` | Second smoothing length forming the histogram. |
| `SignalLength` | Smoothing length of the histogram to create the signal line. |
| `AppliedPrice` | Price source used in indicator calculations. |
| `SignalBarShift` | Number of closed bars to look back when evaluating signals. |
| `Phase` | Reserved parameter kept for compatibility (not used in the current implementation). |

## Signal conditions
* **Breakdown**
  * Long: histogram at `SignalBarShift` is positive while the previous bar is not.
  * Short: histogram at `SignalBarShift` is negative while the previous bar is not.
* **Twist**
  * Long: histogram at `SignalBarShift` is rising after a falling period (previous < latest and two-bars-back > previous).
  * Short: histogram at `SignalBarShift` is falling after a rising period (previous > latest and two-bars-back < previous).
* **CloudTwist**
  * Long: histogram crosses above the signal line (latest histogram > latest signal, previous histogram <= previous signal).
  * Short: histogram crosses below the signal line.

Each signal can both flatten the opposite exposure (if exits are allowed) and open a new trade with the configured volume.

## Risk management
`StartProtection` is initialised with the specified stop loss and take profit distances (converted from points to price units using the instrument's tick size). If either distance is zero the respective protection is omitted. Slippage is also converted to price units using the same tick size.

## Notes
* Signals are processed only on finished candles to mirror the original MetaTrader behaviour.
* `SignalBarShift` allows delaying trade confirmation to avoid acting on the most recent bar.
* The `Phase` parameter is retained for completeness but has no effect when using the supported smoothing methods.
* All code comments are provided in English to simplify future maintenance.
