# LANZ Strategy 4.0 Backtest
[Русский](README_ru.md) | [中文](README_cn.md)

LANZ Strategy 4.0 Backtest is a breakout strategy using swing pivots to detect trend changes. When price breaks above the last pivot high, it enters long; when price breaks below the last pivot low, it enters short. Position size is calculated from risk percent and pip value, with stop loss below/above the last swing plus buffer and take profit by risk-reward ratio.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Price crosses above last pivot high.
  - **Short**: Price crosses below last pivot low.
- **Exit Criteria**: Stop loss or take profit.
- **Stops**: Recent swing high/low with buffer.
- **Default Values**:
  - `SwingLength` = 180
  - `SlBufferPoints` = 50
  - `RiskReward` = 1
  - `RiskPercent` = 1
  - `PipValueUsd` = 10
- **Filters**:
  - Category: Breakout
  - Direction: Long & Short
  - Indicators: Highest, Lowest
  - Complexity: Medium
  - Risk level: Medium
