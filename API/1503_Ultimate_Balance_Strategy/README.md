# Ultimate Balance Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Ultimate Balance Strategy blends ROC, RSI, CCI, Williams %R and ADX into a weighted oscillator. A moving average of this oscillator generates signals: crossing above an oversold level triggers a long, while crossing below an overbought level closes or reverses.

## Details

- **Entry Criteria**: Oscillator MA crosses above `OversoldLevel`.
- **Long/Short**: Both (shorts optional via `EnableShort`).
- **Exit Criteria**: Oscillator MA crosses below `OverboughtLevel`.
- **Stops**: No.
- **Default Values**:
  - `WeightRoc` = 2
  - `WeightRsi` = 0.5
  - `WeightCci` = 2
  - `WeightWilliams` = 0.5
  - `WeightAdx` = 0.5
  - `EnableShort` = false
  - `OverboughtLevel` = 0.75
  - `OversoldLevel` = 0.25
  - `MaType` = SMA
  - `MaLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: ROC, RSI, CCI, WilliamsR, ADX
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
