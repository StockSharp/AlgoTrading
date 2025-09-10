# AI SuperTrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The AI SuperTrend strategy blends the SuperTrend indicator with weighted moving averages of price and the SuperTrend line. A long trade is opened when the SuperTrend turns upward and the price WMA moves above the WMA of the SuperTrend. A short trade is opened on the opposite conditions. Positions are protected with an ATR-based trailing stop.

## Details

- **Entry Criteria**:
  - **Long**: SuperTrend direction flips up and price WMA is above SuperTrend WMA.
  - **Short**: SuperTrend direction flips down and price WMA is below SuperTrend WMA.
- **Exit Criteria**:
  - Trend reversal or ATR trailing stop.
- **Stops**: Dynamic ATR trailing stop.
- **Default Values**:
  - `AtrPeriod` = 10
  - `AtrFactor` = 3
  - `PriceWmaLength` = 20
  - `SuperWmaLength` = 100
  - `EnableLong` = true
  - `EnableShort` = true
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SuperTrend, WMA, ATR
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
