# Color Step Xccx Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the Color Step XCCX indicator. The indicator measures the deviation of price from a smoothed average and plots two step lines. A long trade is opened when the fast line falls below the slow line. A short trade is opened when the fast line rises above the slow line.

## Details

- **Entry Criteria**:
  - Long: fast line crosses below slow line
  - Short: fast line crosses above slow line
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: fast line crosses above slow line
  - Short: fast line crosses below slow line
- **Stops**: None
- **Default Values**:
  - `DPeriod` = 30
  - `MPeriod` = 7
  - `StepSizeFast` = 5
  - `StepSizeSlow` = 30
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Custom, EMA
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
