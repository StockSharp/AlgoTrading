# MTF Seconds Values JD Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy demonstrates handling of custom multi-timeframe candles based on a specified seconds interval. It calculates a simple moving average over aggregated candles and trades on price crossing the average.

## Parameters

- `SecondsTimeframe` – seconds interval for candle aggregation.
- `AverageLength` – period for the simple moving average.
