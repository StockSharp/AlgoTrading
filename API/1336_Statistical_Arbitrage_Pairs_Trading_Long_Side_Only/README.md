# Statistical Arbitrage Pairs Trading - Long-Side Only
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy executes a simple pairs trading approach based on the z-score spread between two instruments. It opens a long position when the spread falls below a user-defined threshold and closes the position when the spread crosses above zero.

## Details

- **Entry Criteria**: Spread z-score below threshold.
- **Long/Short**: Long only.
- **Exit Criteria**: Spread z-score crosses above zero.
- **Stops**: No.
- **Default Values**:
  - `ZScoreLength` = 20
  - `ExtremeLevel` = -1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Long
  - Indicators: SMA, StandardDeviation
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
