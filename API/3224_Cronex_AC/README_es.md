# Cronex AC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Cronex AC recrea el clásico asesor experto Cronex Acceleration/Deceleration (AC) utilizando la API de alto nivel de StockSharp. Suaviza el Oscilador Acelerador con dos medias móviles consecutivas y reacciona cuando la línea rápida cruza la línea lenta. Los cruces alcistas abren posiciones largas y cierran cortas, mientras que los cruces bajistas abren cortas y cierran largas.

## Lógica de trading

1. Construir valores del Oscilador Acelerador (AO-AC) desde la serie de velas seleccionada.
2. Suavizar el AC con el tipo de media móvil elegido dos veces: el primer suavizado produce la línea "rápida" y el segundo suavizado produce la línea "señal".
3. Evaluar las dos líneas en la barra definida por el parámetro `SignalBar`. La estrategia también mira una barra más atrás para confirmar un cruce.
4. Cuando la línea rápida cruza por encima de la línea de señal, la estrategia cierra las posiciones cortas existentes (si está habilitado) y abre una nueva posición larga (si está habilitado).
5. Cuando la línea rápida cruza por debajo de la línea de señal, la estrategia cierra las posiciones largas existentes (si está habilitado) y abre una nueva posición corta (si está habilitado).
6. El tamaño de la posición es igual al `Volume` configurado más el valor absoluto de la posición actual, permitiendo reversiones en una sola orden a mercado.

La lógica refleja al experto MQL5 actuando únicamente en velas completamente terminadas y separando los permisos para entradas y salidas en ambas direcciones.

## Parámetros

| Nombre | Tipo | Valor predeterminado | Descripción |
| --- | --- | --- | --- |
| `SmoothingType` | `CronexMovingAverageType` | `Simple` | Algoritmo de media móvil aplicado al Oscilador Acelerador. Opciones: Simple, Exponential, Smoothed, Weighted. |
| `FastPeriod` | `int` | `14` | Retroceso del primer suavizado (línea rápida). |
| `SlowPeriod` | `int` | `25` | Retroceso del segundo suavizado (línea de señal). |
| `SignalBar` | `int` | `1` | Número de barras terminadas a mirar atrás al leer la señal. Un valor de 1 replica el comportamiento Cronex predeterminado. |
| `CandleType` | `DataType` | `TimeFrame(8h)` | Serie de velas utilizada para los cálculos. |
| `EnableLongEntry` | `bool` | `true` | Permitir abrir posiciones largas después de un cruce alcista. |
| `EnableShortEntry` | `bool` | `true` | Permitir abrir posiciones cortas después de un cruce bajista. |
| `EnableLongExit` | `bool` | `true` | Permitir cerrar posiciones largas cuando la línea rápida cae por debajo de la línea lenta. |
| `EnableShortExit` | `bool` | `true` | Permitir cerrar posiciones cortas cuando la línea rápida sube por encima de la línea lenta. |
| `Volume` | `decimal` | predeterminado de la estrategia | Tamaño de orden utilizado para entradas. La estrategia añade automáticamente el valor absoluto de la posición abierta para revertir en una sola operación. |

## Gráficos

Cuando hay un área de gráfico disponible, la estrategia representa:

- velas fuente para el marco temporal seleccionado,
- valores del Oscilador Acelerador,
- medias móviles rápida y de señal,
- las propias operaciones de la estrategia para validación visual.

## Notas

- Todos los cálculos se basan en velas completadas (`CandleStates.Finished`) para evitar repintado.
- Los buffers de suavizado mantienen exactamente los valores históricos suficientes para evaluar el desplazamiento `SignalBar` solicitado, coincidiendo con el experto MQL original.
- Las características de gestión monetaria de la versión MQL (stop-loss, take-profit, desviación) se omiten intencionalmente para que la gestión de posiciones pueda manejarse externamente a través de los controles de riesgo de StockSharp.
