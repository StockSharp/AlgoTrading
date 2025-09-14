# Color JLaguerre
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the color-coded Laguerre oscillator.

The indicator smooths price movement with a Jurik filter and paints its line according to position within predefined levels. A change of color marks a potential trend shift.

The strategy enters long when the oscillator crosses the middle level upward and short when it crosses downward. Positions are closed when the oscillator reaches extreme levels or an opposite signal appears.

## Details

- **Entry Criteria**: Color change of Laguerre oscillator around middle level.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or reaching extreme level.
- **Stops**: Yes.
- **Default Values**:
  - `RsiLength` = 14
  - `HighLevel` = 85
  - `MiddleLevel` = 50
  - `LowLevel` = 15
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Hourly (1h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
