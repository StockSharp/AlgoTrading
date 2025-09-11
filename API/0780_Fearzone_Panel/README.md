# Fearzone Panel
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy inspired by the FearZone panel from "Framgångsrik Aktiehandel". It looks for panic sell-offs where fear dominates.

The strategy waits for both Fearzone indicators to be active and for at least one panic trigger while price remains above the 200-period moving average.

## Details

- **Entry Criteria**: FZ1 and FZ2 active plus negative impulse, ricochet zone or stochastic oversold, with close above MA200.
- **Long/Short**: Long only.
- **Exit Criteria**: Price falls below MA200.
- **Stops**: No.
- **Default Values**:
  - `LookbackPeriod` = 22
  - `BollingerPeriod` = 200
  - `ImpulsePeriod` = 10
  - `ImpulsePercent` = 0.1m
  - `MaPeriod` = 200
  - `StochThreshold` = 30m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Long
  - Indicators: BollingerBands, RateOfChange, StochasticOscillator, SimpleMovingAverage, Highest
  - Stops: No
  - Complexity: Medium
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
