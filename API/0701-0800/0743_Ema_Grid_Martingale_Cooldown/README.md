# EMA Grid Martingale Cooldown Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implements an EMA-based long-only grid system with optional martingale sizing and cooldown between grids. A new grid starts when both fast EMAs cross above their slower counterparts. Additional buys are placed at fixed pip intervals, and the position is closed at the weighted average price plus a buffer.
