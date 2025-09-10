# BTCUSD Adjustable SLTP Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy trades BTCUSD using a crossover between SMA(10) and SMA(25) with an EMA(150) filter. Long entries wait for a pullback: after the crossover a retracement percentage is tracked and a long position opens when price crosses back above that level. Short entries trigger immediately on a bearish crossover while price is below the EMA.

Exits use adjustable take-profit, stop-loss and break-even distances. A long position is also closed if SMA(10) crosses below SMA(25) while price is under the EMA(150).

## Details

- **Entry Criteria**:
  - Long: SMA(10) crosses above SMA(25), then price retraces by a set percentage and crosses above the retracement level.
  - Short: SMA(10) crosses below SMA(25) while price is below EMA(150).
- **Long/Short**: Long and short.
- **Exit Criteria**:
  - Configurable take profit, stop loss and break-even distances.
  - Long exit when SMA(10) crosses below SMA(25) under EMA(150).
- **Stops**: Yes, adjustable in points.
- **Default Values**:
  - `FastSmaLength` = 10
  - `SlowSmaLength` = 25
  - `EmaFilterLength` = 150
  - `TakeProfitDistance` = 1000
  - `StopLossDistance` = 250
  - `BreakEvenTrigger` = 500
  - `RetracementPercentage` = 0.01
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: SMA, EMA
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
