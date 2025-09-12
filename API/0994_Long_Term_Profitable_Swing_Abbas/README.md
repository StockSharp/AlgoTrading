# Long Term Profitable Swing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long when a fast EMA crosses above a slow EMA and the RSI is above a specified threshold. Exits occur when price hits ATR-based stop loss or take profit levels.

## Details

- **Entry Criteria**:
  - Long: fast EMA crosses above slow EMA and RSI > threshold.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Price reaches ATR-based stop loss or take profit.
- **Stops**: ATR multiples for stop loss and take profit.
- **Default Values**:
  - `FastEmaLength` = 16
  - `SlowEmaLength` = 30
  - `RsiLength` = 9
  - `AtrLength` = 21
  - `RsiThreshold` = 50
  - `AtrStopMult` = 8
  - `AtrTpMult` = 11
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: EMA, RSI, ATR
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
