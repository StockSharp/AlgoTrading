# CANX MA Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades EMA crossovers of the median price (HL2). A long position is opened when the fast EMA crosses above the slow EMA. If long-only mode is disabled, a short position is opened when the fast EMA crosses below the slow EMA. A start year filter prevents trading before the specified year.

## Parameters
- Candle Type
- Fast EMA length
- Multiplier (slow EMA = fast length * multiplier)
- Long only
- Start year
