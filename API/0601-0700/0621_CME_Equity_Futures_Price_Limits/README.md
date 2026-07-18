# CME Equity Futures Price Limits
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy calculates daily price limit levels for CME equity futures. It captures a reference price at a specified hour and computes limit up/down (+/-5%) as well as -7%, -13%, and -20% limit-down levels. Results are written to the log for monitoring.

## Parameters

- **ManualReference** – manual reference price override (0 to disable).
- **ShowLimitDownLevels** – enable logging of -7/-13/-20% levels.
- **OffsetHour** – hour (0-23) to capture the reference price.
- **CandleType** – candle type to process (default 1 minute).
