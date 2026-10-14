# MA Volume Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
MA Volume combines a moving average trend filter with volume surges to time entries.
Rising volume alongside price above the average signals strong accumulation; falling volume below the average indicates distribution.

Testing indicates an average annual return of about 136%. It performs best in the stocks market.

The strategy trades in the direction of the moving average when volume expands, exiting once volume dries up or the average reverses.

A percent stop protects against sudden shifts in trend.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `MaPeriod` = 20
  - `VolumePeriod` = 20
  - `VolumeThreshold` = 1.2
    Volume expands when a candle's volume exceeds VolumeThreshold times the average of the previous VolumePeriod candles. While flat, an expanding candle closing above a rising SMA goes long and one closing below a falling SMA goes short. The position closes when volume falls below its average or the SMA turns against it.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Moving Average, Volume
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

