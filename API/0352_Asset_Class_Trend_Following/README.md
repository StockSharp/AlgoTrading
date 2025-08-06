# Asset Class Trend Following Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy follows trends across multiple asset classes. It applies a simple moving average filter to each security in the universe and rebalances the portfolio on the first trading day of each month. Positions are taken only when price is above the moving average.

Testing indicates an average annual return of about 15%. It performs best on diversified futures portfolios.

At the start of each month, securities trading above their SMA receive an equal allocation of capital. Positions are closed when price falls below the SMA or when capital is reassigned at the next rebalance.

## Details

- **Entry Criteria**: `Close > SMA`
- **Long/Short**: Long only
- **Exit Criteria**: `Close <= SMA` or removed at rebalance
- **Stops**: None; capital is redistributed monthly
- **Default Values**:
  - `SmaLength` = 210
  - `MinTradeUsd` = 50
  - `CandleType` = daily
- **Filters**:
  - Category: Trend following
  - Direction: Long only
  - Indicators: SMA
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Long-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
