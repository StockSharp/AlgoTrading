# Estrategia RoBoost
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación en C# del asesor experto MQL4 original **RoBoostj**.
Opera en un único instrumento usando señales basadas en RSI combinadas con detección
simple de momentum de precio. La estrategia opera en el tipo de vela seleccionado
(por defecto velas de 1 hora).

## Lógica

- Cuando el precio de cierre anterior es mayor que el cierre actual y el valor del RSI
  cae por debajo del umbral **RSI Down**, la estrategia abre una posición corta.
- Cuando el precio de cierre anterior es menor o igual al cierre actual y el valor del
  RSI sube por encima del umbral **RSI Up**, la estrategia abre una posición larga.
- Las posiciones activas se gestionan con las siguientes herramientas de riesgo:
  - Niveles fijos de **Take Profit** y **Stop Loss** medidos en unidades de precio.
  - Trailing stop opcional que se activa cuando la operación avanza en beneficio por la
    distancia **Trail Start**. Tras la activación el precio de stop sigue al precio por
    la distancia **Trail Step**.

## Parámetros

| Nombre          | Descripción                                                   |
|-----------------|---------------------------------------------------------------|
| `CandleType`    | Serie de velas usada para los cálculos.                       |
| `RsiPeriod`     | Período del indicador RSI.                                    |
| `RsiUp`         | Umbral RSI para entradas largas.                              |
| `RsiDown`       | Umbral RSI para entradas cortas.                              |
| `TakeProfit`    | Distancia de take profit desde el precio de entrada (puntos). |
| `StopLoss`      | Distancia de stop loss desde el precio de entrada (puntos).   |
| `UseTrailing`   | Activa la lógica de trailing stop.                            |
| `TrailStart`    | Distancia en puntos para activar el trailing stop.            |
| `TrailStep`     | Distancia en puntos mantenida desde el precio actual cuando
                   el trailing stop está activo.                                  |

Todas las distancias se expresan en unidades de precio absolutas y pueden requerir
ajuste según el tamaño del tick del instrumento.

## Uso

1. Añade la estrategia a tu proyecto o ábrela en StockSharp Designer.
2. Configura los parámetros según tus preferencias de trading.
3. Inicia la estrategia. Se suscribirá automáticamente a la serie de velas elegida
   y gestionará las operaciones basándose en los valores de RSI y los cierres de velas.

La estrategia está diseñada con fines educativos y debe probarse con datos históricos
antes de usarla en mercados en vivo.
