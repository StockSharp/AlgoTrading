# MK Custome Adaptive SuperTrend Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Adaptive SuperTrend that clusters ATR volatility into three levels.
Long trades occur when trend flips up, shorts when it turns down.
Stops use the SuperTrend line with optional percent take profit and stop loss.

- **Long**: Direction changes to uptrend.
- **Short**: Direction changes to downtrend.
- **Exit**: Opposite signal, SuperTrend break, or percent stop.

- Indicators: SuperTrend, ATR.
- Stops: SuperTrend, percent stop-loss.
