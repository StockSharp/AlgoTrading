# Break the Range Bound Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy detects quiet market phases where three moving averages converge within a narrow band. When price finally breaks out above or below this range, the strategy enters in the direction of the breakout and aims to capture the emerging trend.

The system observes the spread between the Fast, Mid and Slow SMAs. If the maximum difference between these averages stays below the configured threshold for a specified number of bars, the market is considered "range bound". The highest high and lowest low of that period define the breakout levels.

Trades are opened when price closes beyond these extremes. Positions are protected by reversing conditions: if price falls back into the range or reaches a multiple of the range width in profit, the position is closed.

## Details

- **Entry Criteria**:
  - **Long**: After a range of `RangeLength` bars where SMA spread is below `ShakeThreshold`, enter when price closes above the highest high of the range.
  - **Short**: Under the same range conditions, enter when price closes below the lowest low of the range.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - **Long**: Close if price returns below the range low or profit exceeds `4 * (range high - range low)`.
  - **Short**: Close if price returns above the range high or profit exceeds `4 * (range high - range low)`.
- **Stops**: Implicit exits based on range boundaries and profit multiple.
- **Default Values**:
  - `FastSma` = 38
  - `MidSma` = 140
  - `SlowSma` = 210
  - `ShakeThreshold` = 250
  - `RangeLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: SMA, Highest, Lowest
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
