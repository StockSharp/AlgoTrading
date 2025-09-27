# MartinGale Scalping Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

SMA(3) crossing SMA(8) triggers entries with martingale-style pyramiding. Additional orders are added each bar until stop or take-profit is reached.

## Details

- **Entry Criteria**: `SMA3` above `SMA8` for longs, below for shorts; new entries added while signal persists.
- **Long/Short**: Configurable via `TradingMode`.
- **Exit Criteria**: Price hits `TakeProfit` or `StopLoss` and opposite SMA relationship.
- **Stops**: Yes, based on slow SMA value.
- **Default Values**:
  - `FastLength` = 3
  - `SlowLength` = 8
  - `TakeProfit` = 1.03
  - `StopLoss` = 0.95
  - `TradingMode` = Long
  - `CandleType` = 5 minutes
  - `MaxPyramids` = 5
- **Filters**:
  - Category: Trend
  - Direction: Configurable
  - Indicators: SMA
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
