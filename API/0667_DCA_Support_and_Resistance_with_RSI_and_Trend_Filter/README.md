# DCA Support and Resistance with RSI and Trend Filter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Dollar-cost averaging strategy using support/resistance levels, RSI, and EMA trend filter. Buys at support in an uptrend when RSI is oversold and sells at resistance in a downtrend when RSI is overbought.

## Details

- **Entry Criteria**:
  - Long: price at support, RSI below oversold, above EMA
  - Short: price at resistance, RSI above overbought, below EMA
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: price reaches resistance or RSI above overbought
  - Short: price reaches support or RSI below oversold
- **Stops**: None
- **Default Values**:
  - `LookbackPeriod` = 50
  - `RsiLength` = 14
  - `Overbought` = 70
  - `Oversold` = 40
  - `EmaPeriod` = 200
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: RSI, EMA, Highest, Lowest
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
