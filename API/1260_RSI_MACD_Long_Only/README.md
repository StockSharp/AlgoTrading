# RSI + MACD Long-Only Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy enters long when RSI crosses above midline with MACD bullish confirmation or when MACD crosses above its signal line while RSI stays above the midline. Exits occur when RSI falls below the midline or MACD crosses below the signal with a non-positive histogram. Optional EMA trend filter and oversold context can refine entries.

## Details

- **Entry Criteria**: RSI crosses above midline with MACD bullish or MACD crosses above signal with RSI above midline
- **Long/Short**: Long only
- **Exit Criteria**: RSI crosses below midline or MACD crosses below signal with histogram ≤ 0
- **Stops**: Optional percent take profit and stop loss
- **Default Values**:
  - `RsiLength` = 14
  - `RsiOversold` = 30
  - `RsiMidline` = 50
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `OversoldWindowBars` = 10
  - `EmaLength` = 200
  - `TakeProfitPercent` = 11.5
  - `StopLossPercent` = 2.5
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: RSI, MACD, EMA
  - Stops: Yes (optional)
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
