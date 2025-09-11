# Long Short Exit Risk Management Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Template strategy showing how to handle long and short positions with percentage-based risk controls. It uses basic price equality triggers and optional time exits.

## Details

- **Entry Criteria**: Close price equals configured long or short value.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss, take profit, or time-based exit after N bars.
- **Stops**: Percentage stop loss and take profit with optional trailing.
- **Default Values**:
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 3
  - `ExitBars` = 10
  - `BarsToWait` = 10
  - `MaxTradesPerDay` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Risk management
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
