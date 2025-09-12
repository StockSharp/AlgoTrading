# MultiLayer Acceleration Deceleration Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy layers up to five long entries using the Acceleration/Deceleration oscillator. A new buy stop is placed above the bar's high each time momentum builds in the direction of the trend identified by fractals and the Alligator teeth. When the oscillator weakens or the trend reverses, all pending orders are cancelled and the position is closed.

## Details

- **Entry Criteria**:
  - Uptrend confirmed when price breaks an up fractal above the Alligator teeth.
  - AC oscillator prints a green bar pattern and the close is above the EMA filter.
  - Up to five stop orders are placed at the activation level.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Trend flips to down.
  - Oscillator turns negative.
- **Stops**: Uses fractal-based stop loss.
- **Default Values**:
  - `EMA Length` = 100.
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: Multiple
  - Stops: Yes
  - Complexity: Complex
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
