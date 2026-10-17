# Fancy Bollinger Bands
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy trades Bollinger Band breakouts. It buys when the close price crosses above the upper band and sells when the close price crosses below the lower band.

## Details

- **Entry Conditions**:
  - **Long**: close crosses above upper band
  - **Short**: close crosses below lower band
- **Exit Conditions**: reverse crossover
- **Type**: Breakout
- **Indicators**: Bollinger Bands
- **Timeframe**: 1 minute (default)
- **Stops**: none

**Verifiable rule and defaults.** Bollinger bands use the last 20 completed closing prices **including the signal candle**, with the population standard deviation multiplied by 2. `BollingerPeriod=20`, `DeviationMultiplier=2`, `CandleType=1 minute`. With no position, open a long when the close crosses from at/below the previous upper band to above the current upper band; open a short when it crosses from at/above the previous lower band to below the current lower band. Close the **entire long** when the close crosses back from at/above the upper band to at/below it; close the **entire short** when it crosses back from at/below the lower band to at/above it. The previous and current bands are each calculated from the closes available at that bar. Only the band re-entry closes an existing position; no stop-loss, take-profit or same-bar reversal is used.
