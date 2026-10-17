# Estrategia Fibonacci y Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Reversión a la media solo en largo con bandas Bollinger y extensión Fibonacci por debajo del rango previo.

Valores iniciales: `BollingerPeriod = 20`, `BollingerDeviation = 2`, `FibLookback = 50`, `FibonacciLevel = 0.618`, `CandleType = 1 minuto`. Solo velas completas. Las bandas usan los últimos 20 cierres, incluida la vela de señal, y desviación estándar poblacional. El rango usa máximo High y mínimo Low de las 50 velas anteriores, excluyendo la señal.

Extensión inferior: `F = previousLow − FibonacciLevel × (previousHigh − previousLow)`. Admite coeficientes positivos superiores a 1, como 1.618. Sin posición, compra estrictamente con `Close < lowerBand` y `Close > F`. Cierra todo el largo con `Close >= upperBand` o `Close >= previousHigh`. La igualdad con F no permite entrar. Sin cortos, órdenes stop ni velas futuras.
