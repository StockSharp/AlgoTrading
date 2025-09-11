# PercentX Trend Follower
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy derived from PercentX Trend Follower by Trendoscope.

The strategy normalizes price distance from a selected band (Keltner or Bollinger) and trades when this oscillator crosses dynamic extreme ranges. ATR is used for stop placement.

## Details

- **Entry Criteria**: Oscillator crossing above upper range for long, below lower range for short.
- **Long/Short**: Both directions.
- **Exit Criteria**: ATR-based stop.
- **Stops**: Initial ATR stop.
- **Default Values**:
  - `BandType` = Keltner
  - `MaLength` = 40
  - `LoopbackPeriod` = 80
  - `OuterLoopback` = 80
  - `UseInitialStop` = true
  - `AtrLength` = 14
  - `TrendMultiplier` = 1
  - `ReverseMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: BollingerBands, KeltnerChannels, ATR, Highest, Lowest
  - Stops: ATR
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

