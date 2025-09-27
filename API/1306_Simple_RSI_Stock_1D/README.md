# Simple RSI Stock Strategy 1D
[Русский](README_ru.md) | [中文](README_cn.md)

This system goes long when RSI drops below an oversold level while price stays above the 200-day SMA. The position uses an ATR-based stop and three profit targets.

## Details

- **Entry Criteria**: RSI below `OversoldLevel` and close above SMA filter.
- **Long/Short**: Long only.
- **Exit Criteria**: ATR stop or hit of any take-profit level.
- **Stops**: Yes.
- **Default Values**:
  - `RsiPeriod` = 5
  - `OversoldLevel` = 30
  - `SmaLength` = 200
  - `AtrLength` = 20
  - `AtrMultiplier` = 1.5
  - `TakeProfit1` = 5
  - `TakeProfit2` = 10
  - `TakeProfit3` = 15
  - `StopLossPercent` = 25
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Oscillator
  - Direction: Long
  - Indicators: RSI, SMA, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
