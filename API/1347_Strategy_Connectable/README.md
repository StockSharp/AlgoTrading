# Strategy Connectable Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

Template strategy that can be connected to external signal sources.
It supports long and short directions and applies percent based stop-loss and take-profit.

## Details

- **Entry Criteria**: external signal
- **Long/Short**: Both
- **Exit Criteria**: external signal or stop-loss/take-profit
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 1 minute
  - `StopLossPercent` = 2%
  - `TakeProfitPercent` = 4%
- **Filters**:
  - Category: Other
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
