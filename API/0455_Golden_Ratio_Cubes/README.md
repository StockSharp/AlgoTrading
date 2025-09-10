# Golden Ratio Cubes Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Golden Ratio Cubes strategy uses Fibonacci mathematics to detect breakouts. It tracks the highest high and lowest low over a lookback window and computes extensions based on the golden ratio (φ ≈ 1.618). When price closes beyond these extensions, the strategy enters in the breakout direction.

## Details

- **Entry Criteria**:
  - Close above golden ratio extension of recent range → Buy.
  - Close below golden ratio extension of recent range → Sell.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite breakout signal.
- **Stops**: None.
- **Default Values**:
  - `Lookback` = 34
  - `Phi` = 1.618
- **Filters**:
  - Category: Breakout
  - Direction: Long & Short
  - Indicators: Highest, Lowest
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
