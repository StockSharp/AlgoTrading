# Estrategia HPCS Inter7
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Hpcs Inter7 es un sistema de ruptura de Bollinger bandas convertido del MetaTrader asesor experto de 4 `_HPCS_Inter7_MT4_EA_V01_We.mq4`. El algoritmo monitorea las bandas Bollinger estándar calculadas en la serie de velas seleccionada. Cuando el precio cruza fuera de las bandas, lo interpreta como una ruptura de impulso y abre una posición en la dirección de la ruptura. Para cada nueva entrada, la estrategia coloca inmediatamente los objetivos de parada de pérdidas y toma de ganancias a una distancia fija del precio de entrada para replicar el comportamiento original del asesor experto.

## Lógica de trading
- **Entrada corta**: Cuando la vela anterior cerró por encima de la banda inferior y la última vela cerrada termina por debajo de la banda inferior, la estrategia abre una venta de mercado. Esto recrea la condición original `Close[0] < LowerBand[0] && Close[1] > LowerBand[1]`.
- **Entrada larga**: Cuando la vela anterior cerró por debajo de la banda superior y la última vela cerrada termina por encima de la banda superior, la estrategia abre una compra de mercado. Esto replica `Close[0] > UpperBand[0] && Close[1] < UpperBand[1]` de la implementación MQL.
- **Operación única por vela**: El algoritmo recuerda la hora de apertura de la vela que generó la última orden. Se ignora una nueva señal en la misma vela para evitar operaciones duplicadas, reflejando la variable de protección `gdt_Candle` de MQL4.
- **Órdenes de protección**: Inmediatamente después de que se abre una nueva posición, la estrategia llama a `SetStopLoss` y `SetTakeProfit` usando la distancia configurada. Ambos se colocan simétricamente alrededor del precio de entrada, por lo que la posición siempre tiene objetivos de riesgo y recompensa predefinidos.

## Parámetros
| Nombre | Descripción | Predeterminado | Optimizable |
| --- | --- | --- | --- |
| `BollingerLength` | Número de velas utilizadas para construir las Bollinger Bandas. | 20 | si |
| `BollingerDeviation` | Multiplicador de desviación estándar para el ancho de bandas Bollinger. | 2 | si |
| `CandleType` | Serie de velas utilizadas para los cálculos (el valor predeterminado es un período de tiempo de 1 minuto). | velas de 1 minuto | No |
| `ProtectionDistancePoints` | Distancia de parada de pérdidas y toma de ganancias expresada en pasos de precio. | 10 | si |

## Notas adicionales
- La estrategia utiliza el nivel alto StockSharp API (`SubscribeCandles().Bind(...)`) y no almacena matrices de historial personalizadas.
- `StartProtection()` se activa al inicio para que la plataforma administre automáticamente las órdenes de protección realizadas por `SetStopLoss` y `SetTakeProfit`.
- El tamaño de la posición está controlado por la propiedad base `Strategy.Volume`, al igual que el asesor experto original que negoció un volumen fijo de un lote.
- La estrategia fue diseñada para instrumentos FX donde se implementó el EA original, pero se puede usar en cualquier valor que proporcione señales de banda Bollinger significativas y un valor `PriceStep` válido.
