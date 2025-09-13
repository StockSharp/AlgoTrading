# MTrainer Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

MTrainer Strategy replicates the MT4 MTrainer script. It opens a position when price reaches a predefined entry line and manages it with stop-loss, take-profit and optional partial close lines. The strategy is designed for manual practice in the visual tester.

## Details

- **Entry Criteria**: price crosses entry line
- **Long/Short**: Both
- **Exit Criteria**: stop loss, take profit, or partial close
- **Stops**: Yes
- **Default Values**:
  - `EntryPrice` = 0
  - `TakeProfitPrice` = 0
  - `StopLossPrice` = 0
  - `PartialClosePercent` = 0
  - `PartialClosePrice` = 0
  - `Volume` = 1
- **Filters**:
  - Category: Utility
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
