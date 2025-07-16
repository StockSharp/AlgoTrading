# MACD RSI Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
MACD RSI combines momentum from the Moving Average Convergence Divergence with overbought/oversold readings from RSI.
When both indicators align, the probability of a sustained move increases.

The strategy enters long when MACD crosses up and RSI rises from oversold, or sells short when MACD crosses down with RSI falling from overbought.

Stops based on a percentage of price help contain losses if the indicators diverge after entry.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MACD, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
