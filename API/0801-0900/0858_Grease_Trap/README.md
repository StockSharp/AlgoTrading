# Grease Trap
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grease Trap uses two Fibonacci-length moving averages and trades on their crossovers with profit targets.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Fast average crosses above the slow average.
  - **Short**: Fast average crosses below the slow average.
- **Exit Criteria**: Profit target or opposite crossover.
- **Stops**: None.
- **Default Values**:
  - `Length1` = 9
  - `Length2` = 14
  - `LongProfit` = 0.02
  - `ShortProfit` = 0.02
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: SMA
  - Complexity: Low
  - Risk level: Medium
