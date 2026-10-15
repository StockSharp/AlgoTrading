# Estrategia AMMA Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia usa el indicador **Modified Moving Average (AMMA)** para capturar cambios de tendencia a corto plazo. Analiza la dirección de la pendiente de AMMA en las velas recientes y abre una posición en la dirección de la tendencia emergente mientras cierra la opuesta.

## Cómo funciona

1. Se calcula una `ModifiedMovingAverage` con un período configurable en el marco temporal seleccionado.
2. En cada vela terminada, la estrategia compara los últimos tres valores de AMMA.
3. Si los valores del indicador forman una secuencia ascendente y el valor más reciente es mayor que el anterior, se abre una posición larga. Se cierra cualquier posición corta.
4. Si los valores del indicador forman una secuencia descendente y el valor más reciente es menor que el anterior, se abre una posición corta. Se cierra cualquier posición larga.

## Parámetros

- `CandleType` – marco temporal de las velas utilizadas para los cálculos.
- `MaPeriod` – período de la media móvil modificada.
- `AllowLongEntry` – habilitar la apertura de posiciones largas.
- `AllowShortEntry` – habilitar la apertura de posiciones cortas.
- `AllowLongExit` – habilitar el cierre de posiciones largas.
- `AllowShortExit` – habilitar el cierre de posiciones cortas.

## Notas

La estrategia opera únicamente en velas completadas y se apoya en los métodos incorporados `BuyMarket` y `SellMarket` para la ejecución de órdenes. Las funciones de gestión de riesgo pueden añadirse externamente usando las propiedades estándar de `Strategy`.
