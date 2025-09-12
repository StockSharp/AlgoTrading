# Reversal Trading Bot Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Reversal Trading Bot Strategy uses RSI divergence with optional volume, ADX, Bollinger Bands, and RSI crossover filters to catch market reversals. Positions are protected with fixed percent stop-loss and take-profit.

## Details

- **Entry Criteria**: RSI divergence with optional volume, ADX, Bollinger Bands, and RSI crossover filters
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or take-profit
- **Stops**: Fixed percent
- **Default Values**:
  - `RsiLength` = 8
  - `FastRsiLength` = 14
  - `SlowRsiLength` = 21
  - `BbLength` = 20
  - `AdxThreshold` = 20
  - `DivLookback` = 5
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 2
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: RSI, ADX, Bollinger Bands, SMA
  - Stops: Fixed
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium

