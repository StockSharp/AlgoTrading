# Triple CCI MFI Confirmed Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long when the fast CCI crosses above zero while the middle and slow CCI remain positive, price is above EMA and MFI exceeds 50. Profit is trailed by EMA after an ATR based activation.

Testing shows moderate performance; it works best during trending markets.

## Details
- **Entry Criteria**:
  - **Long**: Fast CCI crosses above 0, middle CCI > 0, slow CCI > 0, MFI > 50, close above EMA
- **Long/Short**: Long only.
- **Exit Criteria**:
  - **Long**: Close below trailing EMA after activation or low hits ATR stop
- **Stops**: Yes.
- **Default Values**:
  - `StopLossAtrMultiplier` = 1.75
  - `TrailingActivationMultiplier` = 2.25
  - `FastCciPeriod` = 14
  - `MiddleCciPeriod` = 25
  - `SlowCciPeriod` = 50
  - `MfiLength` = 14
  - `EmaLength` = 50
  - `TrailingEmaLength` = 20
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: CCI, MFI, EMA, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium
