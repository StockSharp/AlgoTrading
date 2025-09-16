# Timer Trade
[Русский](README_ru.md) | [中文](README_cn.md)

Timer Trade alternates between long and short positions at fixed time intervals. A timer triggers market orders, and each position is automatically protected with stop-loss and take-profit.

## Details

- **Entry Criteria**: Timer event.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop-loss or take-profit.
- **Stops**: Yes, via StartProtection.
- **Default Values**:
  - `TimerInterval` = TimeSpan.FromSeconds(30)
  - `Volume` = 1
  - `StopLossLevel` = 10 points
  - `TakeProfitLevel` = 50 points
- **Filters**:
  - Category: Timer
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
