# Market Trend Levels Non-Repainting
[Русский](README_ru.md) | [中文](README_cn.md)

EMA crossover strategy that optionally filters trades using RSI. Long positions open when the fast EMA crosses above the slow EMA, while short trades trigger on the opposite cross. When `ApplyExitFilters` is enabled and the RSI filter is active, positions close if the RSI leaves the allowed zone.

## Details

- **Entry Conditions**:
  - **Long**: `Fast EMA` crosses above `Slow EMA` and `RSI > RsiLongThreshold` when enabled
  - **Short**: `Fast EMA` crosses below `Slow EMA` and `RSI < RsiShortThreshold` when enabled
- **Exit Conditions**: Opposite crossover or RSI filter failing when `ApplyExitFilters` is true
- **Type**: Trend-following
- **Indicators**: EMA, RSI
- **Timeframe**: 5 minutes (default)
