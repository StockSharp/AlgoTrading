# Color Laguerre
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy based on the Color Laguerre oscillator.

The Color Laguerre oscillator smooths price series using a Laguerre filter and marks trend direction by color changes. The strategy buys when the oscillator turns bullish and sells when it turns bearish. Extreme levels can force exits if price momentum fades.

## Details

- **Entry Criteria**: Oscillator crossing the middle level.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal or stop.
- **Stops**: Yes.
- **Default Values**:
  - `Gamma` = 0.7m
  - `HighLevel` = 85
  - `MiddleLevel` = 50
  - `LowLevel` = 15
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Oscillator
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (1h)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

