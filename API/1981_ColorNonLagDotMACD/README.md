# Color NonLag Dot MACD Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using MACD indicator with several signal detection modes. The approach is ported from the MQL "Exp_ColorNonLagDotMACD" expert advisor.

## Details

- **Entry Criteria**: Depends on selected mode (zero line breakout, MACD twist, signal line twist, or MACD crossing signal line).
- **Long/Short**: Both directions, can be enabled separately.
- **Exit Criteria**: Opposite signals or configured stop/target.
- **Stops**: Optional percent based stop loss and take profit.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Mode` = `MacdDisposition`
  - `TakeProfitPercent` = 4
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: 4H
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
