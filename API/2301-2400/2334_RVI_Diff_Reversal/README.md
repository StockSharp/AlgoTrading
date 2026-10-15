# RVI Diff Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The strategy trades based on the smoothed difference between the Relative Vigor Index (RVI) and its signal line.
It detects points where this difference stops falling and begins to rise to enter long, and vice versa for short positions.

## Details

- **Entry Criteria**: Slope reversal of the smoothed RVI difference
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `RviLength` = 12
  - `SmoothingLength` = 13
  - `CandleType` = 6-hour candles
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RVI, SMA, EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: 6H
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
