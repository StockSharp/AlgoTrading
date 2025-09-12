# Rsi Stochastic Wma Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining RSI, Stochastic Oscillator and a Weighted Moving Average (WMA).
Buys when RSI is oversold, %K crosses above %D and price is above WMA.
Sells short when RSI is overbought, %K crosses below %D and price is below WMA.

## Details

- **Entry Criteria**:
  - Long: `RSI < 30 && %K crosses above %D && Close > WMA`
  - Short: `RSI > 70 && %K crosses below %D && Close < WMA`
- **Long/Short**: Both
- **Stops**: None
- **Default Values**:
  - `RsiLength` = 14
  - `StochK` = 14
  - `StochD` = 3
  - `WmaLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: RSI, Stochastic, WMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
