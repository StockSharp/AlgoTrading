# 3Commas Bot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Simplified version of the 3Commas Bot strategy. It trades when a fast EMA crosses a slower EMA and manages risk using an ATR based stop. A fixed reward target and optional ATR trailing stop are supported.

## Details

- **Entry Criteria**:
  - **Long**: fast EMA crosses above slow EMA.
  - **Short**: fast EMA crosses below slow EMA.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - ATR stop, optional take-profit, optional ATR trailing stop once a reward threshold is met.
- **Stops**: ATR based.
- **Default Values**:
  - `MaLength1` = 21
  - `MaLength2` = 50
  - `AtrLength` = 14
  - `RnR` = 1
  - `RiskM` = 1
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA, ATR
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
