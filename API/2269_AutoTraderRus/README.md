# AutoTraderRus Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Utility strategy that automates trading session control. It subscribes to 1-minute candles to track market time and keeps trading active only between the specified start and stop times. When the time moves outside this window, the strategy closes any open position and refrains from placing new orders.

## Details

- **Entry Criteria**:
  - None. The strategy does not open positions on its own.
- **Long/Short**: Both (manages existing positions).
- **Exit Criteria**:
  - All positions are closed when current time is outside the session.
- **Stops**: None.
- **Default Values**:
  - `StartHour` = 9
  - `StartMinute` = 30
  - `StopHour` = 23
  - `StopMinute` = 30
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Session Management
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
