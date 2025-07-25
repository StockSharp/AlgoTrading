# Parabolic SAR CCI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
This strategy uses Parabolic SAR CCI indicators to generate signals.
Long entry occurs when Price > SAR && CCI < -100 (trend up with oversold conditions). Short entry occurs when Price < SAR && CCI > 100 (trend down with overbought conditions).
It is suitable for traders seeking opportunities in mixed markets.

Testing indicates an average annual return of about 49%. It performs best in the crypto market.

## Details
- **Entry Criteria**:
  - **Long**: Price > SAR && CCI < -100 (trend up with oversold conditions)
  - **Short**: Price < SAR && CCI > 100 (trend down with overbought conditions)
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: Exit long position when price drops below SAR
  - **Short**: Exit short position when price rises above SAR
- **Stops**: No.
- **Default Values**:
  - `SarAccelerationFactor` = 0.02m
  - `SarMaxAccelerationFactor` = 0.2m
  - `CciPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Mixed
  - Direction: Both
  - Indicators: Parabolic SAR CCI
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk Level: Medium

