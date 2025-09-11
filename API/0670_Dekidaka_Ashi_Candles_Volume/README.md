# Dekidaka-Ashi Candles Volume Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines candle body with smoothed volume using the Dekidaka-Ashi approach. It buys on bullish signals and sells on bearish ones. Candles that span both ranges close open positions.

## Details

- **Entry Criteria**:
  - Strong or weak bullish signal: high above upper range and low above lower range.
  - Strong or weak bearish signal: high below upper range and low below lower range.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal or candle spanning both ranges (uncertainty).
- **Stops**: No.
- **Default Values**:
  - `BodySize` = 1
  - `VolumeSmooth` = 1
  - `CandleType` = 5-minute timeframe
- **Filters**:
  - Category: Pattern
  - Direction: Long & Short
  - Indicators: EMA, Volume
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
