# Z-Score RSI Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Z-Score RSI Strategy calculates RSI on price z-score and uses an EMA of RSI for signals. A long position is opened when RSI crosses above its EMA and a short position when it crosses below.

## Details

- **Entry Criteria**: RSI of z-score crosses its EMA
- **Long/Short**: Both
- **Exit Criteria**: Opposite crossover
- **Stops**: No
- **Default Values**:
  - `ZScoreLength` = 20
  - `RsiLength` = 9
  - `SmoothingLength` = 15
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: SMA, StandardDeviation, RSI, EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
