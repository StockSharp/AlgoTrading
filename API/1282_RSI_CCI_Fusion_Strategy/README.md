# RSI-CCI Fusion Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines standardized RSI and CCI into a single oscillator with dynamic bands.
Buys when the fused value crosses above the lower band and sells or shorts when it crosses below the upper band.

## Details

- **Entry Criteria**: rescaled fusion crosses above lower band for long; crosses below upper band for short
- **Long/Short**: Both (short optional)
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `Length` = 14
  - `RsiWeight` = 0.5
  - `EnableShort` = false
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: RSI, CCI, SMA, StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

