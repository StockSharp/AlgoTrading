# Fibonacci & Bollinger Bands Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Long-only mean reversion using Bollinger Bands and a Fibonacci extension below the preceding swing.

Defaults: `BollingerPeriod = 20`, `BollingerDeviation = 2`, `FibLookback = 50`, `FibonacciLevel = 0.618`, `CandleType = 1 minute`. Use finished candles only. Bollinger bands use the last 20 closes including the signal candle and population standard deviation. The swing is the maximum high and minimum low of the previous 50 finished candles, excluding the signal candle.

The downside extension is `F = previousLow − FibonacciLevel × (previousHigh − previousLow)`. Positive extension coefficients above 1, such as 1.618, are supported. From flat, buy strictly when `Close < lowerBand` and `Close > F`. Close the entire long when `Close >= upperBand` or `Close >= previousHigh`. Equality with F does not enter. No shorts, stop orders or future bars are used.
