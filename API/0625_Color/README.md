# Color Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades based on the perceived lightness of a configured color.
If the color is light (luminance > 0.5) the strategy buys, otherwise it sells.

## Details

- **Entry Criteria**:
  - Long: `Color luminance > 0.5`
  - Short: `Color luminance <= 0.5`
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `ColorHex` = "#f23645"
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**:
  - Category: Other
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
