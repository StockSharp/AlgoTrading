# Williams %R Cross Strategy with 200 MA Filter
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy trades Williams %R crosses around the -50 level with a 200-period SMA trend filter.
Positions close using fixed take profit and stop loss distances.

## Details

- **Entry Criteria**: %R crosses thresholds with price relative to 200 SMA
- **Long/Short**: Both
- **Exit Criteria**: take profit or stop loss
- **Stops**: Yes
- **Default Values**:
  - `WrLength` = 14
  - `CrossThreshold` = 10
  - `TakeProfit` = 30
  - `StopLoss` = 20
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: WilliamsR, SMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

