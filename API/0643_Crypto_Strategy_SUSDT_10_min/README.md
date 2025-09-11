# Crypto Strategy SUSDT 10 min
[Русский](README_ru.md) | [中文](README_cn.md)

A simple EMA crossover strategy that enters long when price closes above the EMA and opens below it, and enters short on the opposite condition. Stop loss and take profit are defined as percentages from the entry price.

## Details

- **Entry Criteria**:
  - **Long**: `close > EMA` and `open < EMA`
  - **Short**: `close < EMA` and `open > EMA`
- **Long/Short**: Both sides.
- **Exit Criteria**: Take profit or stop loss.
- **Stops**: Yes, both take profit and stop loss.
- **Default Values**:
  - `CandleType` = 10 minute
  - `EmaLength` = 24
  - `TakeProfitPercent` = 4
  - `StopLossPercent` = 2
  - `OrderPercent` = 30
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
