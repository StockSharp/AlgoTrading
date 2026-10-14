# Pivot Percentile Trend Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy computes percentile-based trend strength across multiple lookbacks and combines it with a SuperTrend filter. Long trades occur when trend strength is positive and price is above the SuperTrend line. Short trades are taken when trend strength is negative and price is below the SuperTrend.

## Details

- **Indicators**: percentile analysis, SuperTrend
- **Direction**: Both
- **Timeframe**: Any

## Calculation used by this sample

Seven windows of `PercentileLength * 1..7` use nearest-rank high/low quartiles (75%/25%). A window votes +1 when the current high is strictly above its upper quartile and -1 when the current low is strictly below its lower quartile. The current candle belongs to each window.

The sum of all votes is the trend strength. A positive strength with the close above the price SuperTrend enters long; a negative strength with the close below it enters short. An opposing position is fully reversed to `Volume`. Neutral strength or a rejected filter keeps the current position. All windows must be formed; there is no cooldown or protective stop.

Defaults: `PercentileLength=40`, `AtrPeriod=10`, `AtrMultiplier=3`, `CandleType=5m`. SuperTrend uses the platform's Wilder ATR and price bands.
