# Realtime Delta Volume Action
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy tracks the difference between buy and sell volume within each candle. A trade is opened when the volume delta exceeds a threshold.

## Details

- **Entry Criteria**: Delta volume above/below threshold.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `DeltaThreshold` = 100
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Volume Delta
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
