# MA Crossover Demand Supply Zones SLTP Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines a short/long simple moving average crossover with demand and supply zone detection. The system looks for crossovers occurring near recently confirmed demand or supply zones, then enters in the direction of the crossover and manages the position with fixed-percent stop loss and take profit.

## Details

- **Entry Criteria**:
  - Long: short SMA crosses above long SMA near a demand zone.
  - Short: short SMA crosses below long SMA near a supply zone.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Price hits take profit or stop loss levels.
- **Stops**: Percent-based stop loss and take profit.
- **Default Values**:
  - `ShortMaLength` = 9
  - `LongMaLength` = 21
  - `ZoneLookback` = 50
  - `ZoneStrength` = 2
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 2
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA, Highest, Lowest
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
