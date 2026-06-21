# Renko Trend Reversal Strategy V2
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Renko Trend Reversal Strategy V2 trades when renko open crosses renko close. It uses ATR-based renko bricks and applies stop-loss and take-profit levels. Shorts can be disabled.

## Details

- **Entry Criteria**: renko open/close cross with time window
- **Long/Short**: Both (shorts optional)
- **Exit Criteria**: stop loss or take profit
- **Stops**: Yes
- **Default Values**:
  - `RenkoAtrLength` = 10
  - `StopLossPct` = 3
  - `TakeProfitPct` = 20
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Renko
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
