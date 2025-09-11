# Crypto MVRV ZScore Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy applies the MVRV Z-Score concept to detect extremes between market value and realized value.
Positions are opened when the spread z-score crosses predefined thresholds and closed on opposite crossovers.

## Details

- **Entry Criteria**:
  - Long when spread z-score crosses above `LongEntryThreshold`.
  - Short when spread z-score crosses below `ShortEntryThreshold`.
- **Long/Short**: Configurable (`TradeDirection`).
- **Exit Criteria**:
  - Opposite threshold crossover.
- **Stops**: None.
- **Default Values**:
  - `ZScoreCalculationPeriod` = 252
  - `LongEntryThreshold` = 0.382
  - `ShortEntryThreshold` = -0.382
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: SMA, StandardDeviation, Z-Score
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
