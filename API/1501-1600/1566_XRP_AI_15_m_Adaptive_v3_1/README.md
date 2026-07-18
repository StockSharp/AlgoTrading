# XRP AI 15-m Adaptive v3.1 Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy trades XRP on 15-minute candles using a higher time frame trend filter. It selects between small pull-backs, medium volume flushes, or large momentum bursts and applies ATR-based stops, targets, trailing stop and a time-based exit.

## Parameters
- **Risk Mult** – ATR multiplier for initial stop.
- **Small TP** – ATR multiplier for take profit when a small pull-back is chosen.
- **Med TP** – ATR multiplier for take profit when a medium volume flush is chosen.
- **Large TP** – ATR multiplier for take profit when a large momentum burst is chosen.
- **Volume Mult** – multiplier of SMA-20 volume to detect spikes.
- **Trail Percent** – trailing stop percent of ATR from the highest price.
- **Trail Arm** – open gain in ATR multiples before trailing activates.
- **Max Bars** – maximum number of 15-minute bars to hold a position.
- **Candle Type** – candle type used for main calculations.
- **Trend Candle Type** – candle type used for trend filter.
