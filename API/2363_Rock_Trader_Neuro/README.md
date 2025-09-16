# Rock Trader Neuro Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades using Bollinger Bands and a simple neuron.
The last seven Bollinger Band widths are normalized to the range [-1,1] and
combined with fixed weights. The weighted sum is passed through a hyperbolic
tangent activation. A negative output opens a long position, while a positive
output opens a short position. Positions are closed by stop loss or take profit.

## Details

- **Entry Criteria**:
  - Long: neuron output < 0
  - Short: neuron output > 0
- **Long/Short**: Both
- **Exit Criteria**:
  - Stop loss or take profit reached
- **Stops**: Absolute price distance
- **Default Values**:
  - `StopLoss` = 30
  - `TakeProfit` = 100
  - `Lot` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Neural
  - Direction: Both
  - Indicators: Bollinger Bands
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: Yes
  - Divergence: No
  - Risk Level: Medium
