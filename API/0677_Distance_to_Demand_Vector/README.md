# Distance to Demand Vector Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the Distance to Demand Vector indicator. It compares the distances to long and short demand vectors and trades on their crossover.

## Details

- **Entry Criteria**:
  - Long: distance to long vector > distance to short vector
  - Short: distance to long vector < distance to short vector
- **Long/Short**: Both
- **Exit Criteria**:
  - Opposite signal
- **Stops**: None
- **Default Values**:
  - `Length` = 100
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Highest, Lowest
  - Stops: No
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
