# Cnagda Fixed Swing Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A strategy using Heikin Ashi candles with two modes:
- **RSI**: entries when short EMA of RSI crosses long EMA on high volume.
- **Scalp**: entries based on EMA and WMA crossovers of Heikin Ashi close.

Stop loss is placed at recent swing high or low and take profit uses a fixed risk/reward multiple.

## Parameters
- Candle Type
- Trade Logic
- Swing Lookback
- Risk Reward
