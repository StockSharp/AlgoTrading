# Rate of Change Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy uses the Rate of Change indicator to detect bubble conditions and trade zero-line crossovers with dynamic position sizing.

Backtests show stable performance on daily data for major assets.

## Details

- **Entry Criteria**: ROC crosses above or below zero; optional short on bubble break.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `RocLength` = 365
  - `BubbleThreshold` = 180m
  - `StopLossPercent` = 6m
  - `FixedRatioValue` = 400m
  - `IncreasingOrderAmount` = 200m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: RateOfChange
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
