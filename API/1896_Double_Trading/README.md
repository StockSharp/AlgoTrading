# Double Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Pairs trading strategy that opens opposite positions on two correlated instruments and closes them when combined profit reaches a target.

## Details

- **Entry Criteria**: simultaneously open first security and second security in opposite directions
- **Long/Short**: Long & Short
- **Exit Criteria**: combined profit >= ProfitTarget
- **Stops**: No
- **Default Values**:
  - `Volume1` = 1
  - `Volume2` = 1.3
  - `ProfitTarget` = 20
  - `SecondSecurity` = required
- **Filters**:
  - Category: Pair Trading
  - Direction: Hedged
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
