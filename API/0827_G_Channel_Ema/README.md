# G-Channel with EMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining G-Channel channel logic with EMA trend filter.

It buys when the last cross is downward and price is below the EMA. It sells when the last cross is upward and price is above the EMA.

## Details

- **Entry Criteria**: G-Channel state with EMA filter.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `ChannelLength` = 100
  - `EmaLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: G-Channel, EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
