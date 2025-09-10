# Adaptive KDJ (MTF)
[Русский](README_ru.md) | [中文](README_cn.md)

The Adaptive KDJ strategy blends KDJ oscillator values from three timeframes. Each timeframe is smoothed with an EMA and combined using adjustable weights. Trend strength is measured with an SMA of the combined oscillator, which adapts the overbought and oversold levels.

The strategy enters long when the J line is below the adaptive buy level and the K line crosses above the D line. It enters short when the J line is above the adaptive sell level and the K line crosses below the D line.

## Details

- **Entry Criteria**: KDJ cross with J below/above dynamic levels.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `TimeFrame1` = TimeSpan.FromMinutes(1)
  - `TimeFrame2` = TimeSpan.FromMinutes(3)
  - `TimeFrame3` = TimeSpan.FromMinutes(15)
  - `KdjLength` = 9
  - `SmoothingLength` = 5
  - `TrendLength` = 40
  - `WeightOption` = 1
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Stochastic, EMA, SMA
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
