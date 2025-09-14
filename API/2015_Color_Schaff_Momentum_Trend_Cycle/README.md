# Color Schaff Momentum Trend Cycle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy uses the Color Schaff Momentum Trend Cycle (STC) to detect trend reversals when the indicator leaves overbought or oversold zones.

## Details

- **Entry Criteria**:
  - Buy when previous STC color was above the upper zone (>5) and current color drops below 6, closing any short positions.
  - Sell when previous STC color was below the lower zone (<2) and current color rises above 1, closing any long positions.
- **Long/Short**: Both.
- **Exit Criteria**: Reverse signal closes the opposite position.
- **Stops**: No explicit stop loss or take profit.
- **Default Values**:
  - `FastMomentum` = 23
  - `SlowMomentum` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true

