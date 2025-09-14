# Asimmetric Stoch NR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on asymmetric stochastic oscillator lines. The strategy reacts to %K and %D crossovers and supports optional position protection.

The method switches periods for %K calculation to adapt to market noise. Stop loss and take profit are applied in absolute price units.

## Details

- **Entry Criteria**:
  - Long: `%K` crosses above `%D`
  - Short: `%K` crosses below `%D`
- **Long/Short**: Both
- **Exit Criteria**:
  - Long: `%K` crosses below `%D`
  - Short: `%K` crosses above `%D`
- **Stops**: absolute at `StopLoss` and `TakeProfit`
- **Default Values**:
  - `KPeriodShort` = 5
  - `KPeriodLong` = 12
  - `DPeriod` = 7
  - `Slowing` = 3
  - `Overbought` = 80m
  - `Oversold` = 20m
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Stochastic Oscillator
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Long-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

