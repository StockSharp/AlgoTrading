[Русский](README_ru.md) | [中文](README_cn.md)

Reversal Trap Sniper looks for RSI traps where momentum resets but price keeps moving.
It buys after an overbought reversal that still closes higher, and sells after an oversold reversal that still closes lower.

## Details

- **Entry Criteria**: RSI overbought/oversold three bars ago with current RSI crossing back and price continuing in same direction
- **Long/Short**: Both
- **Exit Criteria**: ATR stop or target or maximum bars
- **Stops**: ATR based
- **Default Values**:
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `RiskReward` = 2
  - `MaxBars` = 30
  - `AtrLength` = 14
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: RSI, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
