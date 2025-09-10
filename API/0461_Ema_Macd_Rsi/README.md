# EMA MACD RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining trend filter with EMA, MACD crossovers, and RSI levels.

It buys when fast EMA is above slow EMA, MACD crosses above its signal line, and RSI is between RsiBuyLevel and 70. It sells when fast EMA is below slow EMA, MACD crosses below its signal line, and RSI is between 30 and RsiSellLevel.

## Details

- **Entry Criteria**: Trend filter with EMA, MACD crossover, RSI level.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `FastEmaLength` = 50
  - `SlowEmaLength` = 200
  - `RsiLength` = 14
  - `RsiBuyLevel` = 45m
  - `RsiSellLevel` = 55m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, MACD, RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
