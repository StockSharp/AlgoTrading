# P-Square Nth Percentile Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estimates the selected percentile of the source series using the P-Square algorithm. Opens a long position when the value exceeds the upper percentile and a short position when the value falls below the lower percentile.

## Parameters
- `Percentile` – percentile to estimate.
- `UseReturns` – process returns instead of prices.
- `CandleType` – candle data type.
