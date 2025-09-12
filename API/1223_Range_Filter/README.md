# Range Filter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A range filter based trend following system that uses an adaptive band to enter trades when price crosses the filter.

The strategy calculates a smooth range of price changes and builds an upper/lower band around a dynamic filter. Long trades are opened when price moves above the filter while it is rising. Short trades are taken when price falls below the filter while it is declining. Each position uses fixed risk and reward levels.

## Details

- **Entry Criteria**: Price crossing the range filter in the direction of its slope.
- **Long/Short**: Both directions.
- **Exit Criteria**: Fixed stop loss or take profit.
- **Stops**: Fixed points distance.
- **Default Values**:
  - `Period` = 100
  - `Multiplier` = 3
  - `RiskPoints` = 50
  - `RewardPoints` = 100
  - `UseRealisticEntry` = true
  - `SpreadBuffer` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Custom Range Filter
  - Stops: Fixed Points
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
