# Hull Ma Stochastic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)
 
Hull Moving Average + Stochastic Oscillator strategy. Strategy enters when HMA trend direction changes with Stochastic confirming oversold/overbought conditions.

Hull MA quickly reveals trend direction. Stochastic waits for a dip or rally within that trend to trigger the trade.

A flexible approach for those wanting smooth signals. ATR-based stops cap potential loss.

## Details

- **Entry Criteria**:
  - Long: `HullMA turning up && StochK < 20`
  - Short: `HullMA turning down && StochK > 80`
- **Long/Short**: Both
- **Exit Criteria**:
  - Hull MA change of direction
- **Stops**: ATR-based using `StopLossAtr`
- **Default Values**:
  - `HmaPeriod` = 9
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossAtr` = 2m
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Hull MA, Moving Average, Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
