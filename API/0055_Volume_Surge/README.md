# Volume Surge
[Русский](README_ru.md) | [中文](README_cn.md)
 
Volume Surge identifies unusually high volume relative to the moving average. When the ratio exceeds the defined multiplier, it signals strong interest and potential continuation in the direction of price relative to its moving average.

Trades are initiated only on a surge and closed once volume falls back below average or when the stop-loss is reached.

This simple approach captures momentum sparked by sudden participation.

## Details

- **Entry Criteria**: Volume ratio above `VolumeSurgeMultiplier`.
- **Long/Short**: Both directions.
- **Exit Criteria**: Volume drops below average or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `VolumeAvgPeriod` = 20
  - `VolumeSurgeMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Volume
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
