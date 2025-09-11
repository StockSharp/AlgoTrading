# RedK Slow Smooth WMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses a triple-pass weighted moving average to filter noise. A position is opened when the smoothed average changes direction: long when it turns up, short when it turns down.

## Details

- **Entry Criteria**:
  - **Long**: triple WMA slope turns upward.
  - **Short**: triple WMA slope turns downward.
- **Long/Short**: Both.
- **Exit Criteria**: opposite signal.
- **Stops**: No.
- **Default Values**:
  - `CombinedSmoothness` = 15
  - `CandleType` = 1 minute
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: WeightedMovingAverage
  - Stops: No
  - Complexity: Basic
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
