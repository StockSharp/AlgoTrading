# Delta WPR
[Русский](README_ru.md) | [中文](README_cn.md)

Delta WPR compares a fast and a slow Williams %R oscillator to capture momentum shifts. When the fast value exceeds the slow one and the slow oscillator stays above a threshold level, the strategy opens a long position and closes any short exposure. The opposite configuration – fast below slow with the slow oscillator below the level – triggers a short entry. Every new candle is processed only after completion to avoid noise.

Backtests on 4‑hour data show that the approach performs best in ranging markets where Williams %R oscillates between overbought and oversold zones.

## Details

- **Entry Criteria**:
  - Long: `WPR slow > Level && WPR fast > WPR slow`
  - Short: `WPR slow < Level && WPR fast < WPR slow`
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `FastPeriod` = 14
  - `SlowPeriod` = 30
  - `Level` = -50m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: WilliamsR
  - Stops: No
  - Complexity: Basic
  - Timeframe: 4h
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
