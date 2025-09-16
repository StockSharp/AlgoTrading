# Pending Order
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that places four pending orders around the current bid and ask during specified hours. It continuously maintains buy limit, sell limit, buy stop, and sell stop orders at a configurable distance from market price. Each pending order uses fixed stop-loss and take-profit offsets.

## Details

- **Entry Criteria**: Place pending orders at `Distance` ticks from current bid/ask within allowed hours.
- **Long/Short**: Both directions.
- **Exit Criteria**: Take-profit or stop-loss relative to entry price.
- **Stops**: Yes.
- **Default Values**:
  - `StartHour` = 6
  - `EndHour` = 20
  - `TakeProfit` = 20
  - `StopLoss` = 100
  - `Distance` = 15
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Range
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
