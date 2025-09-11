# Flexible Moving Average Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Adjusts position based on crossovers between the previous period's close and a configurable moving average. A cross below reduces the position by a user-defined percentage, while a cross above restores the full position.

## Details

- **Entry Criteria**:
  - **Initial**: Optional full long on the first bar.
  - **Increase**: Previous close crosses above the moving average → position to 100%.
- **Exit Criteria**:
  - **Decrease**: Previous close crosses below the moving average → reduce by `SellPercentage`.
- **Indicators**:
  - Simple, Exponential, Weighted, Hull or Smoothed moving average.
- **Stops**: None.
- **Default Values**:
  - `MaLength` = 200
  - `SellPercentage` = 100
  - `MaMethod` = SMA
  - `AllowInitialBuy` = true
- **Filters**:
  - Trend-following
  - Single timeframe
  - Indicators: moving averages
  - Stops: none
  - Complexity: Low

