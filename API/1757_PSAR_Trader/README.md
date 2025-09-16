# PSAR Trader
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on Parabolic SAR indicator. PSAR Trader follows the dots of the Parabolic SAR indicator and reacts when price crosses from one side to the other. It opens a long position when price moves above the SAR and a short position when price moves below it. Trading can be restricted to a specific time range, and existing positions can optionally be closed when an opposite signal appears. The strategy also applies take-profit and stop-loss levels measured in ticks.

## Details

- **Entry Criteria**: Price crossing the Parabolic SAR indicator.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal (optional), stop-loss or take-profit.
- **Stops**: Take-profit and stop-loss in ticks.
- **Default Values**:
  - `Step` = 0.001m
  - `Maximum` = 0.2m
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 50
  - `StartHour` = 0
  - `EndHour` = 23
  - `CloseOnOpposite` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Parabolic SAR
  - Stops: Take-profit, Stop-loss
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

