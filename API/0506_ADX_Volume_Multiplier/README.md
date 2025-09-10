# ADX Volume Multiplier Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The ADX Volume Multiplier strategy combines trend strength from the Average Directional Index with a volume surge filter. It enters trades only when the ADX exceeds a threshold, the dominant directional line points to the trend direction, and the current volume surpasses a moving average multiplied by a user-defined factor.

## Details

- **Entry Criteria**:
  - ADX above threshold and DI+ > DI- with volume greater than SMA(volume) * multiplier → long.
  - ADX above threshold and DI- > DI+ with volume greater than SMA(volume) * multiplier → short.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Reverse signal triggers position reversal.
- **Stops**: None.
- **Default Values**:
  - `AdxPeriod` = 21
  - `AdxThreshold` = 26
  - `VolumeMultiplier` = 1.8
  - `VolumePeriod` = 20
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: ADX, Volume SMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
