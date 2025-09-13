# News Trading EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Time-based straddle strategy designed for trading around economic news releases. At a scheduled time the strategy places symmetric buy stop and sell stop orders at a fixed distance from the current price. Orders are updated every candle during the activation window to follow market price. If a position is opened the opposite pending order is canceled and optional take-profit and stop-loss levels manage exits.

## Details

- **Entry Criteria**:
  - During the straddle window, place buy stop at close + Distance * step and sell stop at close - Distance * step.
- **Long/Short**: Both
- **Exit Criteria**: Opposite stop, take-profit/stop-loss or order expiration
- **Stops**: Fixed stop loss and take profit
- **Default Values**:
  - `StartDateTime` = DateTime.Now
  - `StartStraddle` = 0
  - `StopStraddle` = 15
  - `Volume` = 0.01m
  - `Distance` = 55m
  - `TakeProfit` = 30m
  - `StopLoss` = 30m
  - `Expiration` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: News
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Event
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: High
