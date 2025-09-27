# RCI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses the Rank Correlation Index and its moving average to trade crossovers. A long position opens when the RCI rises above its moving average. A short position opens when it drops below. Trade direction can be restricted to long-only or short-only.

## Details
- **Entry Criteria**: RCI crossing its moving average.
- **Long/Short**: Configurable (both, long only, short only).
- **Exit Criteria**: Opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `RciLength` = 10
  - `MaType` = SMA
  - `MaLength` = 14
  - `Direction` = Long & Short
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Configurable
  - Indicators: RCI, MA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
