# The VoVix Experiment Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy analyzes the ratio of fast ATR to slow ATR. When the z-score of this ratio spikes and reaches a local maximum, it enters in the direction of the candle. Positions are closed when the z-score falls below the exit threshold.

## Details

- **Entry Criteria**: VoVix z-score above `EntryZ` and at local maximum
- **Long/Short**: Both
- **Exit Criteria**: VoVix z-score below `ExitZ`
- **Stops**: No
- **Default Values**:
  - `FastAtrLength` = 13
  - `SlowAtrLength` = 26
  - `ZScoreWindow` = 81
  - `EntryZ` = 1.0
  - `ExitZ` = 1.4
  - `LocalMaxWindow` = 6
  - `SuperZ` = 2.0
  - `MinVolume` = 1
  - `MaxVolume` = 2
- **Filters**:
  - Category: Volatility
  - Direction: Both
  - Indicators: ATR, Highest, SMA, StdDev
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
