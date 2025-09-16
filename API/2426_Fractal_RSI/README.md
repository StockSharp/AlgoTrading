# Fractal RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Adaptive strategy based on the Fractal RSI indicator.
Fractal RSI adjusts the length of the RSI calculation using the fractal dimension of price movement,
allowing the oscillator to react faster in trending markets and slower in ranging conditions.

The strategy opens positions when the indicator crosses predefined levels.
It can trade with the detected trend or against it depending on the chosen mode.

## Details

- **Entry Criteria**:
  - *Trend Mode - Direct*:
    - Buy: value crosses below `LowLevel`
    - Sell: value crosses above `HighLevel`
  - *Trend Mode - Against*:
    - Buy: value crosses above `HighLevel`
    - Sell: value crosses below `LowLevel`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: Optional fixed stop-loss and take-profit
- **Default Values**:
  - `CandleType` = `TimeSpan.FromHours(4).TimeFrame()`
  - `FractalPeriod` = 30
  - `NormalSpeed` = 30
  - `HighLevel` = 60
  - `LowLevel` = 40
  - `StopLoss` = 1000 points
  - `TakeProfit` = 2000 points
- **Filters**:
  - Category: Trend / Oscillator
  - Direction: Both
  - Indicators: Fractal Dimension, RSI
  - Stops: Yes
  - Complexity: Advanced indicator usage
  - Timeframe: 4H (configurable)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
