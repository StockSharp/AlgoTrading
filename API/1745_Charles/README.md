# Charles EMA RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy emulates the Charles expert advisor by combining exponential moving averages (EMA) with an RSI filter and a trailing stop. It trades both directions and dynamically protects positions.

The system monitors a fast and a slow EMA on the selected timeframe. When the fast EMA crosses above the slow EMA and RSI exceeds 55, the strategy enters a long position. Conversely, when the fast EMA crosses below the slow EMA and RSI drops below 45, it enters a short position. After entry, a trailing stop follows price to lock in profits while a fixed take profit and stop loss are managed through built-in position protection.

## Details

- **Entry Criteria**:
  - **Long**: `Fast EMA` crosses above `Slow EMA` and `RSI > 55`.
  - **Short**: `Fast EMA` crosses below `Slow EMA` and `RSI < 45`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Trailing stop.
  - Stop-loss or take-profit.
- **Stops**: Uses built-in protection with trailing.
- **Default Values**:
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `RsiPeriod` = 14
  - `TakeProfit` = 0.02
  - `StopLoss` = 0.008
  - `TrailStart` = 0.006
  - `TrailOffset` = 0.003
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: 1 hour by default
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
