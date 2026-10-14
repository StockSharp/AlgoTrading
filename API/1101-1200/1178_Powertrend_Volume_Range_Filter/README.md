# Powertrend - Volume Range Filter Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy uses a volume-adjusted range filter to define dynamic price bands. Long entries occur when price breaks the upper band during an uptrend, and positions close when price falls below the lower band. Optional filters include ADX, high/low trend confirmation, and a VWMA of the filter value.

## Calculation and execution

C# and Python use this explicit causal definition. On finished candles, let `d = abs(Close - previous Close)` and `V = TotalVolume`. After `RangePeriod` changes, `r = RangeMultiplier * sum(d * V) / sum(V)` over the current-inclusive window. The summed volume must be positive. The first formed filter `f` is the current close; thereafter clamp the previous `f` into `[Close - r, Close + r]`. Bands for the current decision are `previous f + r` and `previous f - r`, before updating the center. This avoids a band defined to equal the breakout price after movement.

Enter `Volume` only from flat when `Close > upper band` and the filter rises. Close the entire long by market order when `Close < lower band`, regardless of optional entry filters. Equality holds the position. A pending order blocks additional orders. There are no shorts, EMA crossover or cooldown.

Optional ADX must be formed and strictly above `AdxThreshold`; a disabled ADX is not registered and cannot impose its warmup. High/low confirmation requires both current `High > previous High` and `Low > previous Low`. Filter-value VWMA is `sum(f * V) / sum(V)` over the latest `VwmaPeriod` formed filter values, including the current value; require full formation, positive summed volume and `Close > VWMA`. Disabled filters impose no formation requirement. These are candle-close market decisions, not broker stop orders or assumed intrabar high/low execution.

Defaults: `RangePeriod=40`, `RangeMultiplier=1.5`, `AdxPeriod=14`, `AdxThreshold=20`, `VwmaPeriod=20`, `CandleType=TimeSpan.FromMinutes(5).TimeFrame()`. `IsAdxFilterEnabled`, `IsHighLowFilterEnabled`, `IsVwmaFilterEnabled` all default to `false`. Periods and multiplier must be positive; ADX threshold is within `[0, 100]`. The formula and defaults specify this example; the brief above did not define an original indicator formula.
