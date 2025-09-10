# BTC Difficulty Adjustments Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The BTC Difficulty Adjustments strategy trades based on changes in Bitcoin mining difficulty. When threshold mode is enabled, trades are opened only if the percentage change exceeds the specified threshold. A long position is opened on positive difficulty adjustments, while a short position is opened on negative adjustments.

## Details

- **Entry Criteria**:
  - Threshold mode: `abs(change) >= Threshold` and `change < 0` → enter long.
  - Threshold mode: `abs(change) >= Threshold` and `change > 0` → enter short.
  - Without threshold mode: `difficulty > previous difficulty` → enter long.
  - Without threshold mode: `difficulty < previous difficulty` → enter short.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Opposite signal closes and reverses positions.
- **Stops**: None.
- **Default Values**:
  - `CandleType` = 1 day
  - `ThresholdMode` = false
  - `Threshold` = 10
- **Filters**:
  - Category: Fundamental
  - Direction: Long & Short
  - Indicators: None
  - Stops: No
  - Complexity: Low
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
