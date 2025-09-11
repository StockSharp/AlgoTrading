# Strategy Sema Sdi Webhook Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on smoothed EMA crossover and smoothed directional index confirmation.
Buys when +DI > -DI and fast EMA > slow EMA. Sells when -DI > +DI and fast EMA < slow EMA.

## Details

- **Entry Criteria**:
  - Long: `+DI > -DI && FastEMA > SlowEMA`
  - Short: `+DI < -DI && FastEMA < SlowEMA`
- **Long/Short**: Both
- **Exit Criteria**: Take profit, stop loss, trailing
- **Stops**: TP, SL, trailing
- **Default Values**:
  - `FastEmaLength` = 58
  - `SlowEmaLength` = 70
  - `SmoothLength` = 3
  - `DiLength` = 1
  - `TakeProfitPercent` = 25
  - `StopLossPercent` = 4.8
  - `TrailingPercent` = 1.9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, Directional Index
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
