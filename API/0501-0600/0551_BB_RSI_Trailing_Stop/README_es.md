# BB RSI Trailing Stop

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La excursión fuera de Bollinger requiere recuperación del RSI; después se aplica un stop dinámico condicional. `p` es la vela formada anterior; RSI usa suavizado Wilder.

## Reglas de negociación

Largo: `Low < lowerBand` y `RSI[p] <= RsiOversold && RSI > RsiOversold`. Corto: `High > upperBand` y `RSI[p] >= RsiOverbought && RSI < RsiOverbought`. Permanecer en sobreventa/sobrecompra no basta. Entrada por `Volume` solo sin posición, sin espera oculta.

La protección parte del precio medio ponderado realmente ejecutado y usa bid para largos, ask para cortos. Distancia inicial `StopLossPoints`; el seguimiento solo se activa tras un extremo favorable de `TrailOffsetPoints`. Luego sigue ese extremo a `TrailStopPoints`, sin ampliar el riesgo inicial. Al cruzar la cotización se cierra toda la posición ejecutada a mercado. Son unidades absolutas de precio, no ticks ni porcentajes; no se reutilizan extremos anteriores de una vela para activar un stop creado después.

Las entradas requieren velas terminadas e indicadores preparados. La posición se gestiona hasta su salida; las órdenes de mercado pendientes impiden envíos duplicados.

Bollinger usa la desviación estándar poblacional de la ventana móvil de cierres, incluida la vela terminada actual.

## Parámetros

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `25` |
| `BollingerDeviation` | `2` |
| `RsiPeriod` | `14` |
| `RsiOversold` | `33` |
| `RsiOverbought` | `60` |
| `StopLossPoints` | `50` |
| `TrailOffsetPoints` | `99` |
| `TrailStopPoints` | `40` |
| `CandleType` | `15m` |
