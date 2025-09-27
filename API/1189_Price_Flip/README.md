# Price Flip Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Price Flip strategy mirrors price around recent highs and lows and trades moving average crossovers when the previous close is on the opposite side of this inverted price. A trend filter based on the slow moving average can be applied.

## Details

- **Entry Criteria**:
  - Previous close is above the inverted price.
  - Fast MA crosses above slow MA.
  - Optional: price is above the slow MA when the trend filter is enabled.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal triggers a reversal.
- **Stops**: None.
- **Default Values**:
  - `TickerMaxLookback` = 100
  - `TickerMinLookback` = 100
  - `FastMaLength` = 12
  - `SlowMaLength` = 14
  - `UseTrendFilter` = true
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA, Highest/Lowest
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
