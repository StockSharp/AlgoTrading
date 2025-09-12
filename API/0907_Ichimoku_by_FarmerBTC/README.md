# Ichimoku by FarmerBTC Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Ichimoku by FarmerBTC enters long positions when price trades above the Ichimoku cloud, the cloud is bullish, a higher-timeframe SMA confirms the uptrend, and volume exceeds its moving average multiplied by a factor. It exits when price falls below the cloud.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Long only
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `TenkanPeriod` = 10
  - `KijunPeriod` = 30
  - `SenkouSpanBPeriod` = 53
  - `SmaLength` = 13
  - `VolumeLength` = 20
  - `VolumeMultiplier` = 1.5
  - `CandleType` = 1 hour
  - `HtfCandleType` = 1 day
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: Ichimoku, SMA, Volume
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
