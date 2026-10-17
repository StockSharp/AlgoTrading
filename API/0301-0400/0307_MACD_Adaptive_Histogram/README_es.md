# Estrategia de Histograma Adaptativo MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera valores estadísticamente grandes del histograma MACD. Usa velas completas de 15 minutos y `H = MACD(12,26) − Signal(9)`. Calcula `A = SMA(20)` y `D = StandardDeviation(20)` de H. Espera todos los indicadores formados y `D > 0`.

Sin posición, compra con `H >= A + 2 × D && H > 0`; vende con `H <= A − 2 × D && H < 0`. Cierra todo el largo con `H <= A` y todo el corto con `H >= A`. Cada orden de señal impone una pausa de `CooldownBars = 16` velas completas para nuevas decisiones.

Las posiciones ejecutadas tienen protección nativa local porcentual `StopLossPercent = 2` desde el precio real de entrada. Sigue bid/ask y envía una salida a mercado independiente de la pausa de señales; requiere cotizaciones entre velas. Sin objetivo de beneficios.

`FastPeriod = 12; SlowPeriod = 26; SignalPeriod = 9; HistogramAvgPeriod = 20; StdDevMultiplier = 2; StopLossPercent = 2; CooldownBars = 16`.
