# SuperTrend SDI Webhook Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on SuperTrend and Smoothed Directional Indicator (SDI). It enters long when +DI is above -DI and SuperTrend indicates an uptrend. Short positions open when -DI is above +DI and SuperTrend points down. The strategy applies percent take profit, stop loss and trailing stop.

## Details

- **Entry Criteria**:
  - Long: `+DI > -DI && SuperTrend up`
  - Short: `-DI > +DI && SuperTrend down`
- **Long/Short**: Both
- **Exit Criteria**: Take profit, stop loss or trailing stop
- **Indicators**: SuperTrend, AverageDirectionalIndex
- **Stops**: Percent take profit, stop loss, trailing stop
- **Default Values**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 1.8m
  - `DiLength` = 3
  - `DiSmooth` = 7
  - `TakeProfitPercent` = 25m
  - `StopLossPercent` = 4.8m
  - `TrailingPercent` = 1.9m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SuperTrend, SDI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
