# Anomaly Counter-Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The algorithm detects sharp percentage moves over a short window and trades against them. When price jumps above the threshold it sells; when price drops below the threshold it buys. Stop-loss and take-profit are set in ticks.

## Details

- **Entry Criteria**: Percentage change over lookback window exceeds threshold.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop-loss or take-profit.
- **Stops**: Yes.
- **Default Values**:
  - `PercentageThreshold` = 1
  - `LookbackMinutes` = 30
  - `StopLossTicks` = 100
  - `TakeProfitTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Counter-trend
  - Direction: Both
  - Indicators: Price
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
