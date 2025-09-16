# AML Candle Cross Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on the Adaptive Market Level (AML) indicator.
A trade is opened when the AML value lies inside the current candle body:
if the candle closes above the open and AML is between them, a long
position is opened. For bearish candles the opposite condition opens a
short position. Optionally the position can be reversed when the opposite
signal appears.

## Details

- **Entry Criteria**:
  - **Long**: bullish candle and `open <= AML <= close`.
  - **Short**: bearish candle and `open >= AML >= close`.
- **Long/Short**: Both sides.
- **Exit Criteria**: Position reversed on opposite signal when enabled.
- **Stops**: None.
- **Default Values**:
  - `Fractal` = 70
  - `Lag` = 18
  - `Shift` = 0
  - `UseOpposite` = true
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Single (AML)
  - Stops: No
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
