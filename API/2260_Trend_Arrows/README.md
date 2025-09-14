# Trend Arrows Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts when the closing price moves beyond recent extremes.
It calculates the highest and lowest closing prices over a configurable period.
A new uptrend is detected when the close exceeds the recent high,
while a downtrend starts when the close drops below the recent low.

When a new uptrend is detected, existing short positions can be closed and optional long positions opened.
Conversely, a new downtrend allows closing long positions and optionally opening shorts.
The strategy processes finished candles only and uses StockSharp's high level API.

## Parameters
- **Period** – number of bars to determine recent extremes.
- **Candle Type** – timeframe of candles.
- **Open Long** – allow opening long positions.
- **Open Short** – allow opening short positions.
- **Close Long** – allow closing long positions.
- **Close Short** – allow closing short positions.

## Logic
1. Subscribe to candle data of the selected timeframe.
2. Track highest and lowest closes over the period using `Highest` and `Lowest` indicators.
3. When price breaks above the highest close, signal an uptrend; when below the lowest close, signal a downtrend.
4. Enter or exit positions according to the new trend and enabled options.
