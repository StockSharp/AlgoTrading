# Lorenzo SuperScalp Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This scalping strategy combines RSI, Bollinger Bands and MACD. It buys when RSI is below 45, price is near the lower band and MACD crosses up. It sells when RSI is above 55, price is near the upper band and MACD crosses down. A minimum number of bars between trades prevents rapid re-entry.

## Details

- **Entry Criteria**:
  - **Long**: `RSI < 45` && `Close < LowerBand * 1.02` && `MACD` crosses above signal.
  - **Short**: `RSI > 55` && `Close > UpperBand * 0.98` && `MACD` crosses below signal.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `RSI Length` = 14
  - `Bollinger Length` = 20
  - `Bollinger Multiplier` = 2
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Min Bars` = 15
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Multiple
  - Stops: No
  - Complexity: Medium
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
