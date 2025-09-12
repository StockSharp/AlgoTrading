# RSI Long-Term Strategy 15min
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses RSI oversold signals combined with long-term moving averages and volume confirmation to enter long positions. It buys when RSI is below 30 while the 250-period SMA is above the 500-period SMA and volume is significantly higher than average.

## Details

- **Entry Criteria**: RSI below 30, SMA(250) above SMA(500), and volume greater than 2.5 times its 20-period SMA
- **Long/Short**: Long only
- **Exit Criteria**: SMA(250) crossing below SMA(500) or stop-loss
- **Stops**: Yes, fixed percentage
- **Default Values**:
  - `RsiLength` = 10
  - `VolumeSmaLength` = 20
  - `Sma1Length` = 250
  - `Sma2Length` = 500
  - `VolumeMultiplier` = 2.5
  - `StopLossPercent` = 5
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: RSI, SMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
