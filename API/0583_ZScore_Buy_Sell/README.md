# Z-Score Buy Sell Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy uses Z-score to detect extreme deviations from the moving average.
A position is opened when the z-score crosses above or below a threshold and a cooldown prevents repeated signals.

## Details

- **Entry Criteria**:
  - Short when z-score > `ZThreshold` and sell cooldown passed.
  - Long when z-score < -`ZThreshold` and buy cooldown passed.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Reverse signal.
- **Stops**: None.
- **Default Values**:
  - `RollingWindow` = 80
  - `ZThreshold` = 2.8
  - `CoolDown` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: SMA, StandardDeviation, Z-Score
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
