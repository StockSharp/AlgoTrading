# ICT NY Kill Zone Auto Trading
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades during the New York kill zone using fair value gaps and order blocks.

## Details

- **Entry Criteria**: Fair value gap and order block within kill zone.
- **Long/Short**: Both directions.
- **Exit Criteria**: Position protection.
- **Stops**: Yes.
- **Default Values**:
  - `StopLoss` = 30
  - `TakeProfit` = 60
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Price Action
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

