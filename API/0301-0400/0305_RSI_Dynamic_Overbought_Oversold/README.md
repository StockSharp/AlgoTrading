# RSI Dynamic Overbought Oversold
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trades RSI extremes against adaptive overbought/oversold bands, with a price trend filter. Use finished 5-minute candles. Compute `R = RSI(14)`, `P = SMA(50)` of close, and `A = SMA(50)` and `D = StandardDeviation(50)` of R. Adaptive bounds: `upper = min(A + 2 × D, 85)`, `lower = max(A − 2 × D, 15)`. All indicators must be formed.

From flat, buy if `R <= lower && Close >= P × 0.995`; sell if `R >= upper && Close <= P × 1.005`. Close the entire long if `R >= A || Close < P × 0.995`; close the entire short if `R <= A || Close > P × 1.005`. A signal order starts a pause of `CooldownBars = 48` finished candles for further signal decisions.

Filled positions have local native percentage protection: `StopLossPercent = 2` from the actual entry execution price. It monitors the instrument's bid/ask updates and submits a market exit independently of the signal cooldown; a quote feed is required between candles. No take-profit is configured.

`RsiPeriod = 14; MovingAvgPeriod = 50; StdDevMultiplier = 2; StopLossPercent = 2; CooldownBars = 48`.
