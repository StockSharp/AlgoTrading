# MACD Adaptive Histogram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trades statistically large MACD histogram values. Use finished 15-minute candles and `H = MACD(12,26) − Signal(9)`. Compute `A = SMA(20)` and `D = StandardDeviation(20)` of H. Wait for all indicators to form and `D > 0`.

From flat, buy if `H >= A + 2 × D && H > 0`; sell if `H <= A − 2 × D && H < 0`. Close the entire long when `H <= A`, or the entire short when `H >= A`. Every signal order starts a pause of `CooldownBars = 16` finished candles for further signal decisions.

Filled positions have local native percentage protection: `StopLossPercent = 2` from the actual entry execution price. It monitors the instrument's bid/ask updates and submits a market exit independently of the signal cooldown; a quote feed is required between candles. No take-profit is configured.

`FastPeriod = 12; SlowPeriod = 26; SignalPeriod = 9; HistogramAvgPeriod = 20; StdDevMultiplier = 2; StopLossPercent = 2; CooldownBars = 16`.
