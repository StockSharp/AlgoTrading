# Multi Regression Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades when price crosses a regression line and manages risk with volatility-based bounds. Optional stop loss and take profit levels are derived from a selected risk measure.

## Details

- **Entry Criteria**: Price crossing above or below the regression value.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal or when price reaches selected bounds.
- **Stops**: Optional, based on `UseStopLoss` and `UseTakeProfit`.
- **Default Values**:
  - `Length` = 90
  - `RiskMeasure` = Atr
  - `RiskMultiplier` = 1
  - `UseStopLoss` = true
  - `UseTakeProfit` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: LinearRegression, ATR/StdDev/Bollinger/Keltner
  - Stops: Optional
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
