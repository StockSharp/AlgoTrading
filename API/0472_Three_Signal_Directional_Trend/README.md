# Three Signal Directional Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Three Signal Directional Trend strategy combines MACD, Stochastic oscillator and moving average rate of change to determine trend direction. Each indicator votes for long or short conditions and positions are opened when at least two indicators agree. The method aims to capture broad directional moves while filtering noise using multiple confirmation signals.

## Details

- **Entry Criteria:**
  - At least two out of three signals agree.
  - **Long**: MACD signal rising, Stochastic below oversold, MA ROC positive.
  - **Short**: MACD signal falling, Stochastic above overbought, MA ROC negative.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `AvgLength` = 50
  - `RocLength` = 1
  - `AvgRocLength` = 10
  - `StochLength` = 14
  - `SmoothK` = 3
  - `Overbought` = 80
  - `Oversold` = 20
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdAvgLength` = 9
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MACD, Stochastic, SMA, ROC
  - Stops: None
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
