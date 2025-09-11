# Trend Following KNN Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend Following KNN is a simplified strategy that measures the average price change over a window and compares price with a moving average.
It buys when average change is positive and price is above the moving average, sells when average change is negative and price is below the moving average.

## Details

- **Entry Criteria**: positive/negative average change with price above/below moving average
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `WindowSize` = 20
  - `MaLength` = 50
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
