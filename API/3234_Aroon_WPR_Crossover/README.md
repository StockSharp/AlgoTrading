# Aroon WPR Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy that combines Aroon crossovers with Williams %R momentum filters. A long trade is opened when the fast Aroon Up line crosses above Aroon Down while Williams %R confirms an oversold environment. Short trades follow the inverse logic with Williams %R in overbought territory. Open positions can be closed by Williams %R reversals or by optional stop-loss and take-profit levels measured in price steps.

## Details

- **Entry Criteria**:
  - Long: Aroon Up crosses above Aroon Down and Williams %R < `-(100 - OpenWprLevel)`
  - Short: Aroon Down crosses above Aroon Up and Williams %R > `-OpenWprLevel`
- **Long/Short**: Both
- **Exit Criteria**:
  - Williams %R exits the oversold/overbought zone defined by `CloseWprLevel`
  - Optional take-profit and stop-loss thresholds in price steps
- **Stops**: Optional fixed stop-loss and take-profit in price steps
- **Default Values**:
  - `AroonPeriod` = 14
  - `WprPeriod` = 35
  - `OpenWprLevel` = 20
  - `CloseWprLevel` = 10
  - `TakeProfitSteps` = 0m (disabled)
  - `StopLossSteps` = 0m (disabled)
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Aroon, Williams %R
  - Stops: Optional
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Moderate
