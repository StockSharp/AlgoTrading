# RSI & ADX Long Short Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades both sides using RSI for signals and ADX for trend confirmation.
A long position is opened when RSI crosses above 70 and ADX is above a threshold.
A short position is opened when RSI crosses below 30 and ADX is above the threshold.
Positions are closed on opposite RSI crossovers.

## Details

- **Entry Criteria**: RSI crosses above 70 for longs or below 30 for shorts with ADX above threshold
- **Long/Short**: Both
- **Exit Criteria**: Opposite RSI crossovers
- **Stops**: No
- **Default Values**:
  - `RsiLength` = 8
  - `AdxLength` = 20
  - `AdxThreshold` = 14
- **Filters**:
  - Category: Indicator
  - Direction: Both
  - Indicators: RSI, ADX
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
