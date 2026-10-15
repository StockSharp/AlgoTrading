# DCA Dual Trailing Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy enters long on a fast EMA crossing above a slow EMA. Up to two safety orders are placed when price falls by ATR-based or percentage thresholds. Positions are protected by a standard trailing stop and a secondary lock-in trailing stop enabled after a profit threshold.

## Parameters
- Candle Type
- Fast EMA length
- Slow EMA length
- Use date filter
- Start date
- Use ATR spacing
- ATR length
- ATR SO1 multiplier
- ATR SO2 multiplier
- Fallback SO1 percent
- Fallback SO2 percent
- Cooldown bars
- Base order size USD
- Safety order 1 size USD
- Safety order 2 size USD
- Trailing stop percent
- Lock-in trigger percent
- Lock-in trail percent
