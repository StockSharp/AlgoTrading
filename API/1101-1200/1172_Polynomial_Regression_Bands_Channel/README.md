# Polynomial Regression Bands Channel Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy fits a polynomial regression line to recent prices and builds upper and lower bands from the standard deviation of residuals. Long positions are opened when price falls below the lower band and short positions are opened when price rises above the upper band.

## Details

- **Entry Criteria**:
  - **Long**: `Close < LowerBand`.
  - **Short**: `Close > UpperBand`.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `Length` = 100.
  - `Degree` = 2.
  - `StdDevMultiplier` = 2.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Polynomial regression
  - Stops: No
  - Complexity: Moderate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

## Calculation and execution

Each finished candle fits a least-squares polynomial of `Degree` to the latest `Length` closes, including the current close, on an equally spaced grid from -1 to 1. The channel center is the fitted value at the newest observation. Width is `StdDevMultiplier * sqrt(sum((close[i] - fitted[i])^2) / Length)`: population deviation of residuals, with no degrees-of-freedom correction.

A close strictly below the lower band enters long; strictly above the upper band enters short. An opposite entry fully reverses with one market order of `Volume + abs(Position)`; a flat entry orders `Volume`. Prices inside the bands keep the position. No trading before the full window, no cooldown or protective stop.

Supported settings: `0 <= Degree <= 6`, `Length >= Degree + 2`, positive `StdDevMultiplier`. Degree zero is a constant fit. The fixed normalized grid uses a precomputed orthonormal polynomial basis with two orthogonalization passes; each window projects price offsets onto that basis in double precision. C# and Python use the same calculation; order quantities remain decimal.
