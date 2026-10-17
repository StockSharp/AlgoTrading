# Bollinger Volatility Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza velas de 5 minutos finalizadas y espera a que los indicadores estén formados y la negociación esté permitida. Todas las estadísticas móviles incluyen la vela de señal. `C` es su cierre, `M = SMA(C, 20)`, `D = desviación estándar poblacional(C, 20)`, `U = M + 2 × D`, `L = M − 2 × D` y `A = ATR(14)` con suavizado de Wilder del rango verdadero.

La condición de alta volatilidad es `A >= SMA(A, 14) + 2 × desviación estándar poblacional(A, 14)`. Los niveles de precio son las bandas superior, central e inferior de Bollinger.

Sin posición, compra si se confirma alta volatilidad y `C >= U`; vende si se confirma y `C <= L`. El volumen de entrada es `Volume`. Solo permite una nueva entrada cuando termina la pausa y no hay una orden pendiente. La entrada requiere `A > 0`.

`k = StopLossMultiplier`. Fija el cierre y el ATR de la vela de entrada: el stop largo es `C_entry − k × A_entry` y el corto `C_entry + k × A_entry`. En cada vela finalizada, cierra toda la posición real mediante una orden de mercado si el cierre alcanza el stop fijo, una posición larga cumple `C <= M`, una corta cumple `C >= M` o deja de confirmarse alta volatilidad.

La pausa bloquea nuevas entradas durante las siguientes 84 velas finalizadas tras una orden; las salidas siguen activas. El stop se comprueba al cierre de la vela y la orden de mercado se ejecuta al precio disponible. La distancia del stop permanece fija durante la posición. No hay objetivo de beneficio ni stop dinámico.

## Parámetros predeterminados

```text
BollingerPeriod = 20
BollingerDeviation = 2
AtrPeriod = 14
AtrDeviationMultiplier = 2
StopLossMultiplier = 2
CooldownBars = 84
CandleType = TimeSpan.FromMinutes(5).TimeFrame()
```
