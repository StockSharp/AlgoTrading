# VIDYA Auto-Trading (Reversal Logic) Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy applies a Variable Index Dynamic Average (VIDYA) with wide ATR bands.
A long trade is opened when price breaks above the upper band, and a short trade when price breaks below the lower band.

## Details

- **Entry Criteria**: price crosses ATR band around VIDYA
- **Long/Short**: Both
- **Exit Criteria**: opposite band breakout
- **Stops**: No
- **Default Values**:
  - `VidyaLength` = 10
  - `VidyaMomentum` = 20
  - `BandDistance` = 2
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: VIDYA, ATR
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
