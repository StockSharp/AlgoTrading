# Random State Machine Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Random State Machine Strategy enters trades on random state changes filtered by a moving average. Long positions open when a state change occurs and price is above the moving average. Short positions open when price is below the average. The strategy supports optional take-profit/stop-loss, timed exits, and moving-average cross exits.
