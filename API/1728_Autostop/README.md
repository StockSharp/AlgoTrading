# Autostop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Utility strategy that automatically sets take-profit and stop-loss for open positions.
It does not generate trade signals. Any positions opened externally are protected using fixed distances.

## Details

- **Entry Criteria**: None, orders managed outside the strategy.
- **Long/Short**: Both.
- **Exit Criteria**: Protective orders only.
- **Stops**: Uses StartProtection to place fixed take-profit and stop-loss.
- **Default Values**:
  - `MonitorTakeProfit` = true
  - `MonitorStopLoss` = true
  - `TakeProfitTicks` = 30
  - `StopLossTicks` = 30
- **Filters**:
  - Category: Risk management
  - Direction: Both
  - Indicators: None
  - Stops: Fixed
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
