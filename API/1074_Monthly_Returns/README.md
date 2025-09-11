# Monthly Returns Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Tracks pivot highs and lows to trade breakouts and computes compounded monthly and yearly returns for strategy equity.

## Details

- **Entry Criteria**: Buy when price breaks above latest pivot high; sell when price breaks below latest pivot low.
- **Long/Short**: Both.
- **Exit Criteria**: Positions reverse on opposite signals.
- **Stops**: None.
- **Default Values**:
  - `LeftBars` = 2
  - `RightBars` = 1
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Breakout
  - Direction: Long & Short
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
