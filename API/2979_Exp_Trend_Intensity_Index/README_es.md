# Estrategia Exp Índice de Intensidad de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión de StockSharp del experto de MetaTrader **Exp_Trend_Intensity_Index**. Opera velas finalizadas en un marco temporal configurable y utiliza el Índice de Intensidad de Tendencia (TII) para detectar cuando el momentum sale de zonas alcistas o bajistas extremas. Cuando el indicador hace la transición fuera de una zona superior, el algoritmo cierra cortos y puede iniciar un nuevo largo. Cuando el indicador sale de una zona inferior, el algoritmo cierra largos y puede iniciar un nuevo corto.

## Cómo se construye el indicador

1. Seleccionar la fuente de precio (close, open, variantes ponderadas, precios de seguimiento de tendencia, etc.).
2. Suavizar ese flujo de precios con una primera media móvil (`PriceMaMethod`, `PriceMaLength`).
3. Dividir la diferencia entre el precio y el valor suavizado en flujos positivos y negativos.
4. Suavizar los flujos positivos y negativos de forma independiente con una segunda media móvil (`SmoothingMethod`, `SmoothingLength`).
5. Calcular el Índice de Intensidad de Tendencia: `TII = 100 * Positive / (Positive + Negative)`.
6. Comparar el resultado con los umbrales `HighLevel` y `LowLevel` para asignar un estado de color: zona alta (`0`), neutral (`1`) o zona baja (`2`).

La implementación usa medias móviles de StockSharp (simple, exponencial, suavizada, ponderada). Los tipos de suavizado avanzado de la biblioteca MQL original no están disponibles en este port.

## Lógica de trading

* Las señales se procesan solo cuando una vela está completamente cerrada (`CandleStates.Finished`).
* El parámetro `SignalBar` define qué barra completada se analiza (por defecto un barra hacia atrás). La estrategia también inspecciona la barra inmediatamente anterior, coincidiendo con la búsqueda de doble búfer en el código MQL.
* Cuando la barra más antigua pertenece a la zona alta (`color == 0`):
  * Cerrar cualquier posición corta si `EnableSellExits` es verdadero.
  * Si la barra más reciente salió de la zona alta y `EnableBuyEntries` es verdadero, abrir o revertir a una posición larga.
* Cuando la barra más antigua pertenece a la zona baja (`color == 2`):
  * Cerrar cualquier posición larga si `EnableBuyExits` es verdadero.
  * Si la barra más reciente salió de la zona baja y `EnableSellEntries` es verdadero, abrir o revertir a una posición corta.
* Las órdenes se envían con `BuyMarket` y `SellMarket`. Las reversiones de posición usan el volumen de posición actual más la propiedad `Volume` configurada.
* La protección opcional de stop-loss y take-profit (unidades de precio) se configura a través de `StopLossPoints` y `TakeProfitPoints` y se implementa con `StartProtection`.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Marco temporal usado para el cálculo del indicador y el trading. |
| `PriceMaMethod`, `PriceMaLength` | Tipo y periodo de la media móvil aplicada al flujo de precio base. |
| `SmoothingMethod`, `SmoothingLength` | Tipo y periodo de la media móvil aplicada a los flujos positivos y negativos. |
| `AppliedPrice` | Fuente de precio para el indicador (close, open, median, variantes de seguimiento de tendencia, Demark, etc.). |
| `HighLevel`, `LowLevel` | Umbrales superiores e inferiores que definen las zonas alcista y bajista. |
| `SignalBar` | Número de barras completadas a mirar hacia atrás para confirmación de señal. |
| `EnableBuyEntries`, `EnableSellEntries` | Interruptores que permiten abrir posiciones largas/cortas. |
| `EnableBuyExits`, `EnableSellExits` | Interruptores que permiten salidas automáticas cuando el indicador cambia. |
| `StopLossPoints`, `TakeProfitPoints` | Distancias protectoras opcionales expresadas en unidades de precio para `StartProtection`. |

## Diferencias con el experto MQL original

* Las opciones de gestión del dinero (`MM`, `MMMode`, `Deviation`) se reemplazan con la propiedad de volumen estándar de StockSharp y la ejecución de órdenes; la gestión de deslizamiento no se replica.
* Solo se admiten los tipos de media móvil disponibles en StockSharp (simple, exponencial, suavizada, ponderada).
* Los parámetros de fase del indicador MQL se omiten porque los indicadores de StockSharp no exponen controles equivalentes.
* Las órdenes se ejecutan inmediatamente después de que se confirma una señal en la vela finalizada; no hay programación explícita para la siguiente apertura de barra.

Estos cambios mantienen la idea de trading intacta mientras siguen las directrices de estrategia de alto nivel de StockSharp.
