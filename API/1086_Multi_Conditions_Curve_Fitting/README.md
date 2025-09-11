# Multi Conditions Curve Fitting Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines EMA crossover, RSI and Stochastic oscillator to trade when multiple signals align.

## Details

- **Entry Criteria**:
  - Long: `FastEMA > SlowEMA` and `RSI < RsiOversold` and `StochK < 20`
  - Short: `FastEMA < SlowEMA` and `RSI > RsiOverbought` and `StochK > 80`
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: `FastEMA < SlowEMA` or `RSI > RsiOverbought` or `StochK > StochD`
  - Short: `FastEMA > SlowEMA` or `RSI < RsiOversold` or `StochK < StochD`
- **Stops**: None
- **Default Values**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 25
  - `RsiLength` = 14
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `StochLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA, RSI, Stochastic
  - Stops: No
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
