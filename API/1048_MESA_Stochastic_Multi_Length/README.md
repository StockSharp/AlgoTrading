# MESA Stochastic Multi Length Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

This strategy uses four MESA Stochastic oscillators with different lookback lengths. A long position is opened when all four oscillators are above their moving average trigger. A short position is opened when all four oscillators fall below their triggers.

## Parameters
- `Length1` – lookback for the first oscillator.
- `Length2` – lookback for the second oscillator.
- `Length3` – lookback for the third oscillator.
- `Length4` – lookback for the fourth oscillator.
- `TriggerLength` – smoothing period for the trigger moving averages.
- `CandleType` – candle time frame.
