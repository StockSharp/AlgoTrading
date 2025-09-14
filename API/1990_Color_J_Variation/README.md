# Color J Variation Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy replicating the ColorJVariation Expert Advisor using Jurik Moving Average. It tracks the JMA slope and enters when the direction changes. The strategy supports absolute stop loss and take profit levels.

## Details

- **Entry Criteria**:
  - Long: `PrevSlopeDown && JMA turns up`
  - Short: `PrevSlopeUp && JMA turns down`
- **Long/Short**: Both
- **Exit Criteria**:
  - Opposite reversal signal
- **Stops**: Absolute via `StopLoss` and `TakeProfit`
- **Default Values**:
  - `JmaPeriod` = 12
  - `JmaPhase` = 100
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filters**:
  - Category: Trend reversal
  - Direction: Both
  - Indicators: Jurik Moving Average
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
