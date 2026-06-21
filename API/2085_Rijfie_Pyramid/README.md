# Rijfie Pyramid Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy opens an initial long position when the Stochastic oscillator crosses above a configurable low level. It then adds new positions each time price drops by a fixed percentage while staying above an EMA filter and a minimum price. An optional timer can close all positions at a specified time.

## Parameters
- Candle Type
- Stochastic low level
- Maximum price for first entry
- Minimum allowed price
- EMA period
- Step level in percent
- Close positions at time
- Close hour
- Close minute
