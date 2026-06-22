# Volume Weighted MA Candle
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy builds volume weighted moving averages (VWMA) for candle open and close prices. The relative position of these VWMAs defines a candle "color".

## Trading Logic
1. A candle is **bullish** when VWMA(open) is below VWMA(close).
2. A candle is **bearish** when VWMA(open) is above VWMA(close).
3. When the previous candle is bullish and the current one turns neutral or bearish, the strategy opens a long position and closes any short.
4. When the previous candle is bearish and the current one turns neutral or bullish, the strategy opens a short position and closes any long.

## Parameters
- `VWMA Period` – length used to calculate both volume weighted moving averages.
- `Candle Type` – timeframe of candles used for calculations.

A protective block is enabled by default: 2% take‑profit and 1% stop‑loss.
