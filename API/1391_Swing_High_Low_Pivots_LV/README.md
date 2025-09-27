# Swing High Low Pivots Strategy [LV]
[Русский](README_ru.md) | [中文](README_cn.md)

Trades around confirmed swing highs and lows. When a pivot low appears the strategy places a long limit order at the pivot bar and sets fixed stop and take-profit targets. Pivot highs trigger short setups. An optional moving average filter can restrict trades to the trend direction.

## Details

- **Inputs**:
  - Pivot length.
  - Stop-loss distance in ticks.
  - Take-profit distance in ticks.
  - Second take-profit and double entry switch.
  - Moving-average filter type and length.
- **Long/Short**: Both.
- **Exit**: Fixed stop and up to two profit targets.
- **Filters**:
  - Category: Pattern recognition
  - Direction: Both
  - Indicators: Moving average
  - Stops: Fixed
  - Complexity: High
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
