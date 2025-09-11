# Parabolic SAR with MACD Confirmation
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines the Parabolic SAR indicator with MACD confirmation. A position is opened when price crosses the SAR in a direction supported by MACD, aiming to capture trend reversals.

## Details

- **Entry Criteria**: Price crosses SAR and MACD line is on the same side of its signal line.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover of price/SAR or MACD.
- **Stops**: No.
- **Default Values**:
  - `SarStart` = 0.02m
  - `SarIncrement` = 0.02m
  - `SarMax` = 0.2m
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Parabolic SAR, MACD
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
