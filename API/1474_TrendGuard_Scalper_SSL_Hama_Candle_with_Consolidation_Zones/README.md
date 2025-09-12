# TrendGuard Scalper SSL + Hama Candle with Consolidation Zones Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy combines a simple SSL channel with Hama candle direction. A long position is opened when the close is above the SSL average, the Hama close (EMA 20) is above the long Hama line (EMA 100) and price stays above the Hama close. Short trades use the opposite conditions. ATR is used to mark periods of low volatility as potential consolidation zones.

## Details
- **Entry**: SSL and Hama trend agree with price confirmation.
- **Exit**: fixed take‑profit and stop‑loss percentages.
- **Indicators**: SMA, EMA, ATR.
- **Filters**: consolidation detection only for analysis.
