# Futures Trading Hours RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades only during U.S. futures session hours (08:30–15:00 CT). It uses the Relative Strength Index (RSI) to enter long when the oscillator crosses above an oversold level and enter short when it crosses below an overbought level. At or after 15:00 CT all open positions are closed.

## Details

- **Entry Criteria**:
  - **Long**: RSI crosses above the oversold level during the session
  - **Short**: RSI crosses below the overbought level during the session
- **Long/Short**: Both sides
- **Exit Criteria**:
  - All positions closed at session end (15:00 CT)
- **Stops**: None
- **Default Values**:
  - `RsiLength` = 14
  - `OverSoldLevel` = 30
  - `OverBoughtLevel` = 70
  - `SessionStart` = 08:30
  - `SessionEnd` = 15:00
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RSI
  - Stops: No
  - Complexity: Low
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
