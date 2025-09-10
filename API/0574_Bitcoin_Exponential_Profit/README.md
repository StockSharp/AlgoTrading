# Bitcoin Exponential Profit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy enters long when the fast EMA crosses above the slow EMA. Position size is calculated from a risk percentage of account equity. Exits occur on EMA crossunder, stop loss, take profit, or trailing stop.

## Details

- **Entry Criteria**:
  - Fast EMA crosses above slow EMA → long.
- **Long/Short**: Long only
- **Exit Criteria**:
  - Fast EMA crosses below slow EMA.
  - Stop loss at risk percent.
  - Take profit at risk × reward multiplier.
  - Trailing stop offset from highest price.
- **Stops**: SL, TP, trailing stop
- **Default Values**:
  - Fast EMA length = 9
  - Slow EMA length = 21
  - Risk percent = 1
  - Reward multiplier = 2
  - Trailing stop offset percent = 0.5
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: EMA
  - Stops: SL & TP & Trailing
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
