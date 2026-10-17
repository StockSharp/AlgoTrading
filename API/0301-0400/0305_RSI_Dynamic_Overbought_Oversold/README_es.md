# Estrategia RSI con Sobrecompra/Sobreventa Dinámica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera extremos RSI con límites adaptativos y filtro de tendencia del precio, usando velas completas de 5 minutos. Calcula `R = RSI(14)`, `P = SMA(50)` del cierre y `A = SMA(50)`, `D = StandardDeviation(50)` de R. Límites: `upper = min(A + 2 × D, 85)`, `lower = max(A − 2 × D, 15)`. Todos los indicadores deben estar formados.

Sin posición, compra con `R <= lower && Close >= P × 0.995` y vende con `R >= upper && Close <= P × 1.005`. Cierra todo el largo con `R >= A || Close < P × 0.995`, y todo el corto con `R <= A || Close > P × 1.005`. Cada orden de señal inicia una pausa de `CooldownBars = 48` velas completas para nuevas decisiones.

Las posiciones ejecutadas tienen protección nativa local porcentual `StopLossPercent = 2` desde el precio real de entrada. Sigue bid/ask y envía una salida a mercado independiente de la pausa de señales; requiere cotizaciones entre velas. Sin objetivo de beneficios.

`RsiPeriod = 14; MovingAvgPeriod = 50; StdDevMultiplier = 2; StopLossPercent = 2; CooldownBars = 48`.
