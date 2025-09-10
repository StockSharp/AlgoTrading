# Adaptive Fractal Grid Scalping
[Русский](README_ru.md) | [中文](README_cn.md)

Adaptive Fractal Grid Scalping places limit orders around recent fractal pivots using ATR for distance. The trend is defined by a simple moving average. When volatility exceeds a threshold, buy limits are set below fractal lows in uptrends and sell limits above fractal highs in downtrends. Exits occur at the opposite grid level or on a trailing stop based on ATR.

## Details

- **Entry Criteria**: ATR above threshold with price relative to SMA; buy limit at fractal low minus ATR multiplier or sell limit at fractal high plus ATR multiplier.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite grid level or fractal-based stop.
- **Stops**: Yes.
- **Default Values**:
  - `AtrLength` = 14
  - `SmaLength` = 50
  - `GridMultiplierHigh` = 2.0m
  - `GridMultiplierLow` = 0.5m
  - `TrailStopMultiplier` = 0.5m
  - `VolatilityThreshold` = 1.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Scalping
  - Direction: Both
  - Indicators: Fractal, ATR, SMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
