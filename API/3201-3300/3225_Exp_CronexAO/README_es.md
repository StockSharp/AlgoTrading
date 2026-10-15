# Estrategia de Exp Cronex AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto de MetaTrader **Exp_CronexAO** a la API de alto nivel de StockSharp. El robot original opera cruces entre las dos líneas del Cronex Awesome Oscillator (AO). La versión StockSharp se suscribe a una serie de velas configurable, calcula el AO, lo suaviza dos veces con medias móviles para reproducir las líneas Cronex, y abre o cierra posiciones cuando la línea rápida cruza la línea lenta.

## Lógica de trading

1. Construir el Awesome Oscillator desde las velas seleccionadas.
2. Suavizar el oscilador dos veces con medias móviles simples. El primer suavizado crea la línea Cronex "rápida", el segundo suavizado produce la línea "señal".
3. Mirar atrás `SignalBar` velas completadas y comparar las líneas Cronex en esa barra y en la anterior.
4. Una señal de **compra** aparece cuando la línea rápida está por encima de la línea lenta y realizó un cruce ascendente en la barra de retroceso. La estrategia opcionalmente cierra cualquier posición corta y, si se permite, abre una orden de compra a mercado.
5. Una señal de **venta** refleja la regla anterior: la línea rápida debe estar por debajo de la línea lenta y debe haber cruzado hacia abajo en la barra de retroceso. La estrategia opcionalmente cierra cualquier posición larga y, si se permite, abre una orden de venta a mercado.
6. Los niveles de stop-loss y take-profit, expresados en puntos del instrumento, se adjuntan a la posición resultante siempre que se abre una nueva operación.

Solo se mantiene una posición neta. Cuando la dirección cambia, la estrategia combina el volumen necesario para cerrar la posición opuesta con el nuevo volumen de trade para emular el modo de netting de MetaTrader.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Tipo de datos de las velas utilizadas para los cálculos de Cronex AO. El valor predeterminado es un marco temporal de 8 horas. |
| `FastPeriod` | Longitud del primer suavizado aplicado al Awesome Oscillator. |
| `SlowPeriod` | Longitud del segundo suavizado aplicado a la línea rápida. |
| `SignalBar` | Número de barras completadas hacia atrás que deben contener la señal de cruce. La estrategia también inspecciona la siguiente barra para confirmar la dirección. |
| `BuyOpenEnabled` / `SellOpenEnabled` | Habilitar o deshabilitar la apertura de posiciones largas o cortas. |
| `BuyCloseEnabled` / `SellCloseEnabled` | Controlar si las posiciones opuestas pueden cerrarse cuando aparece una señal inversa. |
| `TakeProfit` | Objetivo de beneficio en puntos, aplicado después de cada nueva entrada si es mayor que cero. |
| `StopLoss` | Stop protector en puntos, también aplicado después de cada nueva entrada si es mayor que cero. |

## Gestión de riesgo

Las distancias de stop-loss y take-profit imitan las entradas basadas en puntos de la versión MetaTrader. Se recalculan cada vez que se envía una nueva operación para que las órdenes protectoras siempre coincidan con el tamaño de la posición neta actual.

## Diferencias de la versión MetaTrader

- La implementación StockSharp usa medias móviles simples para ambas etapas de suavizado Cronex. La implementación XMA original permite varios métodos de suavizado, pero la configuración predeterminada corresponde a la media simple que se reproduce aquí.
- Las rutinas de deslizamiento y gestión monetaria de la biblioteca `TradeAlgorithms` no se replican. El dimensionamiento de posición se controla mediante la propiedad estándar `Volume`.
- La ejecución de operaciones depende del comportamiento de netting de StockSharp. Cuando la dirección se revierte, se emite una única orden a mercado con suficiente volumen para aplanar y cambiar la posición en un paso, reflejando la lógica de cuenta de netting de MT5.
