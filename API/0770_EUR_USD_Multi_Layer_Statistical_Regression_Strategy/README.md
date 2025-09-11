# EUR/USD Multi-Layer Statistical Regression Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that uses multiple linear regression layers to estimate trend direction on EUR/USD. It calculates short, medium and long regressions, validates them by R² and slope thresholds and trades in the direction of the weighted ensemble.

## Details

- **Entry Criteria**:
  - Long: weighted slope > 0 and reliability > 0.5
  - Short: weighted slope < 0 and reliability > 0.5
- **Long/Short**: Both
- **Exit Criteria**: Reverse when opposite signal appears
- **Stops**: Daily loss protection
- **Default Values**:
  - `ShortLength` = 20
  - `MediumLength` = 50
  - `LongLength` = 100
  - `MinRSquared` = 0.45m
  - `SlopeThreshold` = 0.00005m
  - `WeightShort` = 0.4m
  - `WeightMedium` = 0.35m
  - `WeightLong` = 0.25m
  - `PositionSizePct` = 50m
  - `MaxDailyLossPct` = 12m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Linear Regression
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Risk Level: Medium
