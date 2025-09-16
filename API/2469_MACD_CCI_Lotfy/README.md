# Macd Cci Lotfy Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining Moving Average Convergence Divergence (MACD) and Commodity Channel Index (CCI) with a scaling factor.
A position is opened when both indicators cross extreme thresholds in the same direction.

The MACD value is multiplied by a coefficient to align the scale with CCI, allowing direct comparison with the same threshold.
The approach aims to capture overbought and oversold reversals.

## Details

- **Entry Criteria**:
  - Long: `CCI < -Threshold` and `MACD * MacdCoefficient < -Threshold`
  - Short: `CCI > Threshold` and `MACD * MacdCoefficient > Threshold`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal triggers reverse position
- **Stops**: None
- **Default Values**:
  - `CciPeriod` = 8
  - `FastPeriod` = 13
  - `SlowPeriod` = 33
  - `MacdCoefficient` = 86000
  - `Threshold` = 85
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: MACD, CCI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Short term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

