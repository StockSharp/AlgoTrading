# Exp Candles XSmoothed Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy monitors candle highs and lows smoothed by a weighted moving average (WMA). When the closing price breaks above the smoothed high plus a configurable buffer, it opens a long position and closes any existing short. Conversely, a close below the smoothed low minus the buffer opens a short and closes any existing long.

## Parameters
- **MA Length** – number of periods for the weighted moving averages applied to highs and lows.
- **Level** – breakout buffer in points added to the smoothed high and subtracted from the smoothed low.
- **Candle Type** – timeframe of the candles used for analysis.
- **Buy Open / Sell Open** – permissions to open long or short positions.
- **Buy Close / Sell Close** – permissions to close existing positions when an opposite breakout occurs.

The strategy draws smoothed high and low lines on the chart for visual confirmation and uses built‑in position protection once started.
