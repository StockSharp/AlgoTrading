# MA L World Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Weighted moving average crossover strategy with trailing stop based on EMA.

Opens long when the fast WMA crosses above the slow WMA. Opens short when the fast WMA crosses below the slow WMA. Uses a 92-period EMA as a trailing exit and fixed stop-loss and take-profit levels.

## Details

- **Entry Criteria**:
  - Long: `Fast WMA` crosses above `Slow WMA`
  - Short: `Fast WMA` crosses below `Slow WMA`
- **Long/Short**: Both
- **Exit Criteria**: Opposite crossover or price crossing trailing EMA
- **Stops**: Stop loss and take profit via `StartProtection`
- **Default Values**:
  - `FastMaLength` = 12
  - `SlowMaLength` = 25
  - `TrailingMaPeriod` = 92
  - `StopLoss` = 95m
  - `TakeProfit` = 670m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: WMA, EMA
  - Stops: Stop loss, take profit, trailing EMA
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
