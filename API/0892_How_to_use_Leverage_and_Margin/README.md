# How to Use Leverage and Margin Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A Stochastic oscillator crossover system. The strategy buys when the %K line crosses above %D below the 80 level and sells short when %K crosses below %D above 20. Positions are protected by a take‑profit measured in ticks.

## Details

- **Entry Criteria**:
  - **Long**: %K crosses above %D and %K < 80.
  - **Short**: %K crosses below %D and %K > 20.
- **Long/Short**: Both
- **Exit Criteria**: Take‑profit or opposite crossover
- **Stops**: Yes, take‑profit in ticks
- **Default Values**:
  - `Stochastic Period` = 13
  - `%K Period` = 4
  - `%D Period` = 3
  - `Take Profit Ticks` = 100
  - `CandleType` = 1 minute
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Stochastic
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

