# Fine-tune Inputs Gann + Laplace Smooth Volume Zone Oscillator Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy uses a volume oscillator smoothed by exponential moving averages.
A long position is opened when the smoothed oscillator rises above the threshold.
A short position is opened when it falls below the negative threshold.
If signals disappear and **Close All** is enabled, any open position is closed.

## Parameters
- **Fast Volume EMA** – period for fast volume average.
- **Slow Volume EMA** – period for slow volume average.
- **Smooth Length** – smoothing period for oscillator.
- **Threshold** – signal level for entries.
- **Close All** – close position when no signal.
- **Candle Type** – candle type used for calculations.
