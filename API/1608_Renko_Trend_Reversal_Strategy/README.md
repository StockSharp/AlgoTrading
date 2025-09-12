# Renko Trend Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Renko Trend Reversal Strategy trades when renko open crosses renko close. Stop-loss and take-profit can be enabled. Uses ATR-based renko bricks.

## Details

- **Entry Criteria**: renko open/close cross with time window
- **Long/Short**: Both
- **Exit Criteria**: optional stop loss or take profit
- **Stops**: Optional
- **Default Values**:
  - `RenkoAtrLength` = 10
  - `StopLossPct` = 10
  - `TakeProfitPct` = 50
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR
  - Stops: Optional
  - Complexity: Basic
  - Timeframe: Renko
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
