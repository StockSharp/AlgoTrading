# Donky MA TP SL
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades moving average crossovers with two take-profit targets and a stop-loss. It enters long when the fast SMA crosses above the slow SMA and short when it crosses below. Half of the position is closed at the first target and the remainder at the second target or the stop-loss.

## Details

- **Entry Criteria**:
  - **Long**: Fast SMA crosses above slow SMA.
  - **Short**: Fast SMA crosses below slow SMA.
- **Long/Short**: Both.
- **Exit Criteria**: Two fixed take-profit levels or a fixed stop-loss.
- **Stops**: Yes.
- **Default Values**:
  - `FastLength` = 10
  - `SlowLength` = 30
  - `TakeProfit1Pct` = 0.03m
  - `TakeProfit2Pct` = 0.06m
  - `StopLossPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
