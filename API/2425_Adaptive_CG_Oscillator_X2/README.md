# Adaptive CG Oscillator X2 Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Uses the Adaptive CG Oscillator on two different timeframes.
The higher timeframe defines the prevailing trend while the lower timeframe
handles actual entries and exits based on oscillator crossovers.

## Details

- **Entry Criteria**:
  - Long: oscillator crosses below its signal line while global trend is up
  - Short: oscillator crosses above its signal line while global trend is down
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal or explicit close flags
- **Stops**: No
- **Default Values**:
  - `TrendAlpha` = 0.07m
  - `SignalAlpha` = 0.07m
  - `TrendCandleType` = TimeSpan.FromHours(6).TimeFrame()
  - `SignalCandleType` = TimeSpan.FromMinutes(30).TimeFrame()
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Adaptive CG Oscillator
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Multi-timeframe
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
