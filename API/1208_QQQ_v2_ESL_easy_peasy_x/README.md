# QQQ Strategy v2 ESL easy-peasy-x
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades QQQ using a main moving average crossover with trend filters. It buys when the close price crosses above the main MA while the MA is rising and price is above a long-term trend MA. It sells short when the close crosses below the main MA while the MA is falling and price is below a short-term trend MA.

## Details

- **Entry Criteria**:
  - **Long**: Close crosses above main MA, MA slope rising, price above long trend MA.
  - **Short**: Close crosses below main MA, MA slope falling, price below short trend MA.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `Main MA Length` = 200
  - `Trend Long Length` = 100
  - `Trend Short Length` = 50
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Moving averages
  - Stops: No
  - Complexity: Medium
  - Timeframe: Medium
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

