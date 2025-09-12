# PulseWave Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using VWAP, MACD crossover and RSI filter.

The strategy buys when price is above VWAP, MACD crosses above its signal line and RSI is below the overbought threshold. It exits when price falls below VWAP, MACD crosses below the signal line and RSI is above the oversold threshold.

## Details

- **Entry Criteria**: Price above VWAP, MACD crossover up, RSI below overbought.
- **Long/Short**: Long only.
- **Exit Criteria**: Price below VWAP, MACD crossover down, RSI above oversold.
- **Stops**: No.
- **Default Values**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: VWAP, MACD, RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
