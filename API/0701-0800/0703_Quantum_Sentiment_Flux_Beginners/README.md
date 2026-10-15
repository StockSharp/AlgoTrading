# Quantum Sentiment Flux (Beginners) Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy enters long when the fast EMA crosses above the slow EMA and the difference between them exceeds an ATR-based threshold. It enters short on the opposite signal. Positions are exited when price moves an ATR multiple against the trade or reaches a profit target of two ATR multiples. A cooldown period limits trade frequency.

## Parameters
- Candle Type
- Fast EMA length
- Slow EMA length
- ATR length
- ATR multiplier
- MA strength threshold
- Cooldown bars
- Quantity
