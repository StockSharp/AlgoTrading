# Quantitative Trend Strategy Uptrend Long
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy buys when the price closes above the most recent pivot high detected over configurable lookback windows. Support and resistance levels are taken from pivot highs and lows. Positions are protected by percent-based take-profit and stop-loss.

## Details

- **Entry Criteria**:
  - **Long**: Close price crosses above last pivot high.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Close price crosses below last pivot low.
  - Last pivot high becomes lower than last pivot low.
- **Stops**: Yes, percent take-profit and stop-loss.
- **Default Values**:
  - `PivotLeft` = 46
  - `PivotRight` = 32
  - `StopLossPercent` = 1.75
  - `TakeProfitPercent` = 2
