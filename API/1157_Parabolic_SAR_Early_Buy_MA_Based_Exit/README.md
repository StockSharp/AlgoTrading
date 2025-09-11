# Parabolic SAR Early Buy MA Exit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the Parabolic SAR indicator to enter trades when the indicator switches sides relative to price. A simple moving average provides an additional exit rule: long positions are closed when price drops below the moving average while SAR is above price.

## Details

- **Entry Criteria**: SAR switches sides relative to price.
- **Long/Short**: Both.
- **Exit Criteria**: For long positions, exit when SAR > price and price < MA.
- **Stops**: Not defined.
- **Default Values**:
  - `Acceleration` = 0.02
  - `AccelerationStep` = 0.02
  - `MaxAcceleration` = 0.2
  - `MaPeriod` = 11
  - `CandleType` = 5 minute
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Parabolic SAR, SMA
  - Stops: None
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
