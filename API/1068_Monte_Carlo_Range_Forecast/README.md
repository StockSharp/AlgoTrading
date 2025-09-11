# Monte Carlo Range Forecast
[Русский](README_ru.md) | [中文](README_cn.md)

Monte Carlo Range Forecast uses Monte Carlo simulations with ATR-based volatility to project future price range. The strategy enters long when the average simulated price exceeds the current price and enters short when it falls below.

## Details
- **Data**: Price candles with ATR.
- **Entry Criteria**:
  - **Long**: Expected price from simulations is above current price.
  - **Short**: Expected price from simulations is below current price.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `ForecastPeriod` = 20
  - `Simulations` = 100
- **Filters**:
  - Category: Statistical
  - Direction: Long & Short
  - Indicators: ATR
  - Complexity: Medium
  - Risk level: Medium
