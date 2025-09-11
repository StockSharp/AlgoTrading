# Double CCI Confirmed Hull MA Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy goes long when price crosses above the Hull Moving Average with confirmation from fast and slow CCI indicators. A trailing EMA manages profit after an ATR based activation.

Testing shows moderate annual return. It performs best in mixed markets.

## Details
- **Entry Criteria**:
  - **Long**: Price crosses above HMA, close above HMA, fast CCI > 0, slow CCI > 0
- **Long/Short**: Long only.
- **Exit Criteria**:
  - **Long**: Close below trailing EMA after activation or low hits ATR stop
- **Stops**: Yes.
- **Default Values**:
  - `StopLossAtrMultiplier` = 1.75
  - `TrailingActivationMultiplier` = 2.25
  - `FastCciPeriod` = 25
  - `SlowCciPeriod` = 50
  - `HullMaLength` = 34
  - `TrailingEmaLength` = 20
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Reversal
  - Direction: Long
  - Indicators: CCI, HMA, EMA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
