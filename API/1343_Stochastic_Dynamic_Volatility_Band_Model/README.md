# Stochastic-Dynamic Volatility Band Model Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses Bollinger-style volatility bands to trade crossovers and exits after a fixed number of candles.

## Details

- **Entry Criteria**: long when price crosses above the lower band; short when price crosses below the upper band
- **Long/Short**: Both
- **Exit Criteria**: position closed after `ExitBars` candles
- **Stops**: No
- **Default Values**:
  - `Length` = 5
  - `Multiplier` = 1.67
  - `ExitBars` = 7
- **Filters**:
  - Category: Volatility
  - Direction: Both
  - Indicators: BollingerBands
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
