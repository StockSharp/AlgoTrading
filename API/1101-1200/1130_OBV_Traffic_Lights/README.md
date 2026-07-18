# OBV Traffic Lights Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Uses a Heikin Ashi based On-Balance Volume and three EMAs colored like traffic lights. Long when OBV and the fast EMA are above the slow EMA; short when both are below. Positions close when conditions disappear.

- **Entry Criteria**: OBV > Slow EMA and Fast EMA > Slow EMA for long; OBV < Slow EMA and Fast EMA < Slow EMA for short.
- **Exit Criteria**: Opposite signal or loss of agreement.
- **Indicators**: OBV, EMA, Highest/Lowest
