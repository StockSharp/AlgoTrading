# Equivolume Bars Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategy based on volume spikes relative to the sum of volumes over a lookback period.

## Logic
- Calculate ratio of current volume to the previous sum of volumes.
- Go long when ratio exceeds threshold and candle is bullish.
- Go short when ratio exceeds threshold and candle is bearish.
- Close position when ratio drops below threshold or candle reverses.

## Parameters
- `Lookback` – number of bars for volume sum.
- `Volume Threshold` – ratio threshold for high volume.
- `Candle Type` – type of candles to use.
