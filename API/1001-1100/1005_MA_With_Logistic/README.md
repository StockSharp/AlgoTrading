# MA With Logistic
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Despite its legacy name, this implementation is an EMA crossover strategy; it does not calculate a logistic model. Finished-candle crossovers open or reverse the position, and percentage take-profit and stop-loss protection is measured from execution prices.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: the fast EMA crosses above the slow EMA.
  - **Short**: the fast EMA crosses below the slow EMA.
- **Exit Criteria**: `TakeProfitPercent` or `StopLossPercent` is reached; an opposite crossover reverses the position.
- **Cooldown**: five finished candles after each crossover order.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 25
  - `TakeProfitPercent` = 8
  - `StopLossPercent` = 5
  - `CandleType` = 20 minutes
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: MA
  - Complexity: Low
  - Risk level: Medium
