# Yesterday's High Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Long breakout strategy that places a buy stop above the previous day's high.
Optional ROC filter, trailing stop and EMA close provide additional risk control.

## Details

- **Entry Criteria**: Close below yesterday's high, then buy stop at high + gap
- **Long/Short**: Long only
- **Exit Criteria**: Stop-loss, take-profit, optional trailing stop or EMA cross
- **Stops**: Yes, percentage based
- **Default Values**:
  - `Gap` = 1
  - `StopLoss` = 3
  - `TakeProfit` = 9
  - `UseRocFilter` = false
  - `RocThreshold` = 1
  - `UseTrailing` = true
  - `TrailEnter` = 2
  - `TrailOffset` = 1
  - `CloseOnEma` = false
  - `EmaLength` = 10
  - `CandleType` = 1 minute
- **Filters**:
  - Category: Breakout
  - Direction: Long
  - Indicators: Price, ROC, EMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
