# Adaptive RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Adaptive RSI strategy derives a smoothing coefficient from the Relative Strength Index. When RSI deviates from the neutral 50 level, the coefficient increases, making the adaptive RSI follow price more closely. Near 50, the coefficient shrinks and the curve smooths. A long position is opened when the adaptive RSI turns up, while a short position is opened when it turns down.

## Details

- **Entry Criteria**:
  - Adaptive RSI crosses above its previous value.
  - Adaptive RSI crosses below its previous value.
- **Long/Short**: Both long and short trades.
- **Exit Criteria**:
  - Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `Length` = 14
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: RSI
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
