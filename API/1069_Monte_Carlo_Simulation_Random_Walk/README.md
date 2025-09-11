# Monte Carlo Simulation - Random Walk Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This sample strategy performs a Monte Carlo simulation of future price paths using historical log returns. It does not place trades but demonstrates how to generate random walks and estimate future max and min price levels.

## Details

- **Entry Criteria**: none, this strategy does not trade.
- **Long/Short**: none.
- **Exit Criteria**: not applicable.
- **Stops**: none.
- **Default Values**:
  - `NumberOfBarsToPredict` = 50.
  - `NumberOfSimulations` = 500.
  - `DataLength` = 2000.
  - `KeepPastMinMaxLevels` = false.
- **Filters**: not applicable.
- **Complexity**: medium.
- **Timeframe**: configurable.

