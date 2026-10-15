# Simple DCA Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy places a base order and adds safety orders when price deviates by a specified percentage. It exits once price reaches a take profit calculated from the average entry price. Each safety order size is multiplied by a factor.

## Parameters
- Candle Type
- Base order size (quote currency)
- Price deviation for safety order (%)
- Maximum safety orders
- Take profit (%)
- Order size multiplier
