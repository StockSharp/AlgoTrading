# Classic Nacked Z-Score Arbitrage Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy trades the spread between two assets using the Z-Score. When the spread's z-score rises above a positive threshold the strategy sells the first asset and buys the second. When the z-score drops below the negative threshold it buys the first asset and sells the second. Positions are closed when the z-score reverts toward zero.

## Parameters
- Candle Type
- Lookback Period
- Z-Score Threshold
- Second Security
