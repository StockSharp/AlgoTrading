# Lux Clara EMA + VWAP Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Lux Clara EMA + VWAP strategy trades the crossover of a fast and slow EMA, filtered by VWAP and a time window. A long position is opened when the fast EMA crosses above the slow EMA while the slow EMA is above the VWAP during the session. A short position is opened on the opposite conditions. Positions are closed when the EMAs cross in the opposite direction.

## Details

- **Entry Criteria**:
  - Fast EMA crosses above slow EMA, slow EMA above VWAP, and current time within session.
  - Short: fast EMA crosses below slow EMA, slow EMA below VWAP, and current time within session.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite EMA cross.
- **Stops**: None.
- **Default Values**:
  - `FastEmaLength` = 8
  - `SlowEmaLength` = 50
  - `StartTime` = 07:30
  - `EndTime` = 14:30
  - `CandleType` = 5-minute
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: EMA, VWAP
  - Stops: None
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
