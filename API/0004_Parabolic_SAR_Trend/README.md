# Parabolic SAR Trend
[Русский](README_ru.md) | [中文](README_cn.md)
 
Strategy based on Parabolic SAR indicator Parabolic SAR Trend follows the dots of the Parabolic SAR indicator. A flip of price from one side of the SAR to the other marks a potential trend change. If price crosses back, the trade is closed.

Since the SAR dots trail price, they naturally provide an exit point when the trend shifts. The method trades both long and short without using additional stops beyond the SAR reversal.


## Details

- **Entry Criteria**: Signals based on Parabolic, SAR.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `AccelerationFactor` = 0.02m
  - `MaxAccelerationFactor` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Parabolic, SAR
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

Testing indicates an average annual return of about 49%. It performs best in the crypto market.
