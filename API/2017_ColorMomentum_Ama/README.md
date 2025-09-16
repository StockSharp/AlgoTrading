# Color Momentum AMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy converts the MetaTrader Expert Advisor *Exp_ColorMomentum_AMA* to StockSharp.
It calculates price momentum over a configurable period and smooths it with the Kaufman Adaptive Moving Average (AMA).
Trading signals are generated when the smoothed momentum shows two consecutive rises or falls.

## Logic
- **Long entry**: Momentum AMA rises for two bars in a row. Any existing short position is closed before opening a new long.
- **Short entry**: Momentum AMA falls for two bars in a row. Any existing long position is closed before opening a new short.
- Opposite signals close current positions.

## Parameters
- Candle type
- Momentum period
- AMA period
- Fast period
- Slow period
- Signal bar
