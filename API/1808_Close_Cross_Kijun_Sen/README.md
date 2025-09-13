# Close Cross Kijun Sen Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy acts as a trade management tool. It closes existing positions when the closing price crosses the Kijun-sen line of the Ichimoku indicator.

During execution the strategy subscribes to candles and calculates the Kijun-sen value. When a long position is present and price drops below the Kijun line by a configurable offset, the position is closed. When a short position is open and price rises above the line, the position is also closed. The strategy does not open new trades.

## Details

- **Entry Criteria**: The strategy does not open new trades; it only manages existing positions.
- **Long/Short**: Both (closing).
- **Exit Criteria**: Closing price crossing the Kijun-sen line by the specified offset.
- **Stops**: None.
- **Default Values**:
  - `KijunPeriod` = 50
  - `PointsToCross` = 0
  - `CandleType` = 5 minute
- **Filters**:
  - Category: Trade management
  - Direction: Both
  - Indicators: Ichimoku
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
