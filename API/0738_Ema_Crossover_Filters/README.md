# EMA Crossover Strategy with Filters
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses multiple exponential moving averages (EMAs) to trade crossovers with additional trend filters.

The strategy buys when the 100 EMA crosses above the 200 EMA while the 9 EMA is above the 50 EMA. It sells short when the 100 EMA crosses below the 200 EMA and the 9 EMA is below the 50 EMA. Long positions exit when the 100 EMA crosses below the 50 EMA; short positions exit when the 100 EMA crosses above the 50 EMA.

## Parameters
- Candle Type
- EMA 9 length
- EMA 50 length
- EMA 100 length
- EMA 200 length
