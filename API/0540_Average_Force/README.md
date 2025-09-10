# Average Force Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Average Force strategy uses an oscillator that measures where the close lies within the highest high and lowest low of a lookback period and smooths the result with a moving average. Positive values signal upward pressure while negative values show downward force.

The strategy goes long when the smoothed Average Force value is above zero and goes short when it is below zero.

## Details

- **Entry Criteria**:
  - Average Force > 0 → Buy.
  - Average Force < 0 → Sell.
- **Long/Short**: Both long and short positions.
- **Exit Criteria**:
  - Position reverses when Average Force crosses zero in the opposite direction.
- **Stops**: None.
- **Default Values**:
  - `Period` = 18
  - `Smooth` = 6
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Highest, Lowest, SMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
