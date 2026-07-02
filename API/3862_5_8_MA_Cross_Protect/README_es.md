# 5/8 EMA Estrategia de protección cruzada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **estrategia de protección cruzada EMA 5/8** replica el asesor experto MetaTrader `5_8macrossv2.mq4` al comparar dos promedios móviles configurables en el mismo símbolo. Un cruce alcista del promedio móvil rápido por encima del lento abre posiciones largas, mientras que un cruce bajista abre posiciones cortas. La versión portada sigue StockSharp patrones de alto nivel y agrega administración opcional de obtención de ganancias, stop-loss y trailing-stop.

## Lógica de trading
- Se calculan dos medias móviles sobre la suscripción de vela seleccionada. De forma predeterminada, una MA exponencial de 5 períodos en precios de cierre se compara con una MA exponencial de 8 períodos en precios de apertura.
- Cuando la MA rápida cruza por encima de la MA lenta en la última vela terminada, la estrategia abre o revierte a una posición larga. Si una posición corta está activa, su volumen se incluye en la nueva orden de compra del mercado para invertir la dirección.
- Cuando la MA rápida cruza por debajo de la MA lenta, la estrategia abre o invierte en una posición corta usando la misma lógica de normalización de volumen.
- Los parámetros de desplazamiento MA emulan el desplazamiento horizontal original. Los valores positivos retrasan la señal tantas velas cerradas; los valores negativos se redondean a cero porque los valores desplazados hacia adelante no están disponibles en los datos en tiempo real.

## Gestión del riesgo
- **Las distancias de toma de ganancias** y **stop-loss** se expresan en pips (escalones de precio). Cuando se abre una posición larga, los niveles de protección se colocan por encima y por debajo del precio de entrada respectivamente; La lógica refleja los cortos.
- **El trailing stop** (también en pips) ajusta constantemente el nivel de protección a medida que el precio se mueve a favor de la posición. En el caso de posiciones largas, el trailing stop sólo se mueve hacia arriba; para los cortos, solo se mueve hacia abajo.
- Si se cumple alguna condición de protección en una vela terminada (obtención de ganancias alta, stop-loss baja o nivel de seguimiento), la estrategia sale de la posición con una orden de mercado y restablece su estado interno.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `0.1` | Volumen de pedidos para nuevas entradas. La estrategia agrega el tamaño absoluto de la posición al revertir. |
| `TakeProfitPips` | `decimal` | `40` | Distancia desde la entrada en pips para cerrar la posición con beneficio. Establezca en `0` para desactivar. |
| `StopLossPips` | `decimal` | `0` | Distancia desde la entrada en pips para stop-loss de protección. Establezca en `0` para desactivar. |
| `TrailingStopPips` | `decimal` | `0` | Distancia del trailing-stop en pips. Establezca en `0` para desactivar. |
| `FastPeriod` | `int` | `5` | Período de la media móvil rápida. |
| `FastShift` | `int` | `-1` | Desplazamiento horizontal para el MA rápido. Los valores negativos se tratan como cero en este puerto. |
| `FastMethod` | `MovingAverageMethod` | `Exponential` | Algoritmo de suavizado para el MA rápido (Simple, Exponencial, Suavizado, LinearWeighted). |
| `FastPrice` | `AppliedPrice` | `Close` | Precio de vela utilizado para el MA rápido. |
| `SlowPeriod` | `int` | `8` | Período de la media móvil lenta. |
| `SlowShift` | `int` | `0` | Desplazamiento horizontal para la MA lenta. |
| `SlowMethod` | `MovingAverageMethod` | `Exponential` | Algoritmo de suavizado para el MA lento. |
| `SlowPrice` | `AppliedPrice` | `Open` | Precio de vela utilizado para la MA lenta. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(30).TimeFrame()` | Serie de velas utilizadas para los cálculos. |

## Notas
- La conversión mantiene la lógica centrada en las velas terminadas para evitar señales prematuras.
- Los trailingstops y los objetivos de ganancias se calculan con `Security.PriceStep`; si un símbolo no lo define, los parámetros de riesgo permanecen inactivos.
- La versión de Python se omite intencionalmente según los requisitos de la tarea.
