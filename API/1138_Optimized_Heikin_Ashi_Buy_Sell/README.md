# Optimized Heikin-Ashi Strategy with Buy/Sell Options
[Русский](README_ru.md) | [中文](README_cn.md)

Heikin-Ashi candles smooth price data and highlight trend direction. This strategy trades a single direction at a time: either longs on green candles or shorts on red ones within a user-defined date range. Optional stop-loss and take-profit levels provide risk control.

## Details

- **Entry Criteria**: Heikin-Ashi candle color change.
- **Long/Short**: Configurable.
- **Exit Criteria**: Opposite signal or stop levels.
- **Stops**: Optional, percentage based.
- **Default Values**:
  - `CandleType` = 1 day
  - `StartDate` = 2023-01-01
  - `EndDate` = 2024-01-01
  - `TradeType` = BuyOnly
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
- **Filters**:
  - Category: Trend
  - Direction: Configurable
  - Indicators: Heikin-Ashi
  - Stops: Optional
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: Date range
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

