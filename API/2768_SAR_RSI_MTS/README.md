# SAR RSI MTS Strategy

## Overview

The **SAR RSI MTS Strategy** is a direct translation of the original MetaTrader 5 expert advisor "SAR RSI MTS" into the StockSharp high-level API. The system follows the direction of the Parabolic SAR indicator and confirms entries with the Relative Strength Index (RSI). It works on completed candles only (default timeframe is 1 hour) and respects a configurable cap on the net position size.

## Indicators and Data

- **Parabolic SAR** (`Acceleration = SarStep`, `AccelerationStep = SarStep`, `AccelerationMax = SarMax`).
- **Relative Strength Index** with customizable period and neutral level (default 50).
- Candles supplied by `CandleType`, which defaults to hourly time frame data.

Internally the strategy computes a pip value from the security metadata. If the symbol has 3 or 5 decimal places it multiplies the price step by 10, matching the pip handling of the original MQL program.

## Entry Logic

A new trade is evaluated at the close of each finished candle once both indicators have produced valid values:

- **Long setup**
  1. Parabolic SAR value from the previous bar is below the current close and the current SAR has increased compared to the previous value.
  2. RSI is above the neutral threshold and is rising compared to its previous reading.
  3. If the account is already net short the strategy first buys enough volume to flip the position and then opens a new long sized according to the `Volume` parameter, respecting the `MaxPosition` limit.

- **Short setup**
  1. Previous Parabolic SAR value is above the current close and the current SAR has decreased.
  2. RSI is below the neutral threshold and is falling compared to its previous value.
  3. Existing long exposure is flattened before establishing the new short. Additional shorts are allowed until the absolute position reaches `MaxPosition`.

All comparisons use the instrument precision so that equality tests match the original `CompareDoubles` helper from MQL.

## Exit and Risk Management

Risk controls are evaluated before checking for new entries on every finished candle:

- **Fixed stop-loss** in pips converted into price units and applied to the average entry price of the current net position.
- **Fixed take-profit** in pips, handled symmetrically to the stop-loss.
- **Trailing stop** that becomes active only after unrealized profit exceeds `TrailingStop + TrailingStep`. The stop is moved in discrete steps, mimicking the "Trailing" routine from the MQL strategy.
- If none of the above applies the trailing state is reset whenever the position becomes flat.

All exits close the entire net position (long or short). When a protective rule triggers, the strategy skips signal evaluation for the same bar, mirroring the behaviour of broker-side stop orders in the original implementation.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `StopLossPips` | Stop-loss distance expressed in pips. A value of `0` disables the protective stop. |
| `TakeProfitPips` | Take-profit distance in pips. Disabled when set to `0`. |
| `TrailingStopPips` | Distance of the trailing stop. Disabled when set to `0`. |
| `TrailingStepPips` | Minimum price improvement required before the trailing stop is advanced. |
| `SarStep` | Acceleration step for Parabolic SAR; also used as the initial acceleration factor. |
| `SarMax` | Maximum acceleration factor for Parabolic SAR. |
| `RsiPeriod` | Lookback period for the RSI indicator. |
| `RsiNeutralLevel` | RSI threshold separating bullish and bearish bias (default 50). |
| `CandleType` | Candle subscription used for calculations (default 1 hour). |
| `MaxPosition` | Maximum absolute net position allowed by the strategy. |

## Additional Notes

- The default configuration reproduces the original EA inputs: 10 pip stop, 40 pip target, 15/5 pip trailing stop, Parabolic SAR `0.05/0.5`, and RSI period `14`.
- Volume is controlled by the base `Strategy.Volume` property. Position scaling honours `MaxPosition` and automatically handles reversals.
- Indicator bindings and order routing rely entirely on the StockSharp high-level API without manual series access, ensuring compliance with the project guidelines.
