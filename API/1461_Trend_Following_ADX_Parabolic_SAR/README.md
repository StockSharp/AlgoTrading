# Trend Following ADX Parabolic SAR
[Русский](README_ru.md) | [中文](README_cn.md)

Uses ADX with directional movement and Parabolic SAR to follow trends. Long positions occur when ADX is above a threshold, +DI exceeds -DI, and price is above the SAR line. Short signals use the opposite setup.

## Details

- **Entry Criteria**: ADX > threshold with DI crossover and price > SAR.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25
  - `SarStep` = 0.02
  - `SarMax` = 0.2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: ADX, Parabolic SAR
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
