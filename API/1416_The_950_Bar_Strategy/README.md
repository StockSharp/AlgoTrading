# The 950 Bar Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades the 9:50 AM New York five-minute candle. After the bar completes, it enters in the bar's direction with fixed profit target and stop defined in ticks.

## Details
- **Entry Criteria**: Direction of 9:50 AM NY five-minute bar.
- **Long/Short**: Both directions.
- **Exit Criteria**: Reach target or stop.
- **Stops**: Fixed stop and target.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `TickSize` = 0.25
  - `TargetTicks` = 150
  - `StopTicks` = 200
- **Filters**:
  - Category: Time
  - Direction: Both
  - Indicators: None
  - Stops: Fixed
  - Complexity: Easy
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
