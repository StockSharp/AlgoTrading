# Estrategia JFatl Candle MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea el comportamiento del Expert Advisor original **Exp_JFatlCandle_MMRec.mq5** dentro del framework StockSharp.
Analiza los cambios de color producidos por el filtro de velas JFatl y los combina con un bloque adaptativo de gestión de dinero
que reduce el tamaño de la posición tras un número configurable de pérdidas recientes.

## Idea de trading

* Construye velas sintéticas filtrando los valores OHLC clásicos con el kernel de la Fast Adaptive Trend Line (FATL).
  La implementación usa la tabla de coeficientes original de 39 taps seguida de una etapa de suavizado exponencial para
  aproximar la media móvil Jurik utilizada en MetaTrader.
* Detecta transiciones de color del cuerpo de la vela sintética:
  * color **2** (alcista) significa que el cierre filtrado está por encima de la apertura filtrada;
  * color **0** (bajista) significa que el cierre filtrado está por debajo de la apertura filtrada;
  * color **1** marca un cuerpo neutral.
* Un color alcista en la barra con `SignalBar + 1` períodos de antigüedad obliga a la estrategia a cerrar cualquier corto y prepararse
  para una nueva entrada larga cuando la barra con `SignalBar` períodos de antigüedad ya no es alcista.
* Un color bajista observado de la misma manera cierra los largos y habilita una entrada corta cuando la barra más reciente ya no es bajista.
* Las posiciones largas y cortas se dimensionan mediante la lógica de MMRecounter. Cuando las últimas `TotalTrigger` operaciones de la
  dirección correspondiente incluyen al menos `LossTrigger` resultados negativos, la estrategia cambia al tamaño de posición reducido.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco temporal de las velas que se alimentan al filtro FATL (por defecto: 12 horas).
| `SignalBar` | Número de barras completadas para mirar hacia atrás al leer el buffer de colores. `0` significa usar la barra terminada actual, `1` reproduce los valores predeterminados de MT5.
| `SmoothingLength` | Longitud del suavizado exponencial aplicado después del kernel FATL para emular el suavizado Jurik.
| `NormalVolume` | Tamaño de posición predeterminado cuando el historial reciente es saludable.
| `ReducedVolume` | Tamaño de posición aplicado después de que el MMRecounter detecta demasiadas pérdidas.
| `BuyTotalTrigger` / `SellTotalTrigger` | Cantidad de operaciones históricas (por dirección) inspeccionadas por el MMRecounter.
| `BuyLossTrigger` / `SellLossTrigger` | Número mínimo de pérdidas dentro de la ventana inspeccionada que fuerza el tamaño de posición reducido.
| `EnableBuyEntries` / `EnableSellEntries` | Permitir la apertura de posiciones largas/cortas.
| `EnableBuyExits` / `EnableSellExits` | Permitir el cierre de posiciones largas/cortas cuando aparece la señal opuesta.
| `StopLossPoints` | Stop de protección opcional para ambas direcciones expresado en pasos de precio del instrumento. Establecer en `0` para desactivar.
| `TakeProfitPoints` | Objetivo de beneficio opcional en pasos de precio. Establecer en `0` para desactivar.

## Reglas de trading

1. Construir los valores OHLC filtrados y determinar el color de la vela en cada barra terminada.
2. Sea `C1` el color de la barra de `SignalBar + 1` períodos atrás y `C0` el color de la barra de `SignalBar` períodos atrás
   (para `SignalBar = 0` la barra actual se usa como `C0` y la anterior como `C1`).
3. Si `C1 == 2` (alcista):
   * cerrar cualquier posición corta cuando `EnableSellExits` es `true`;
   * abrir una posición larga con el tamaño calculado cuando `EnableBuyEntries` es `true` **y** `C0 != 2`.
4. Si `C1 == 0` (bajista):
   * cerrar cualquier posición larga cuando `EnableBuyExits` es `true`;
   * abrir una posición corta cuando `EnableSellEntries` es `true` **y** `C0 != 0`.
5. Las posiciones también pueden cerrarse por límites de stop-loss o take-profit cuando el rango de la vela toca el nivel configurado.

## Gestión de dinero

La estrategia almacena el beneficio de cada operación larga y corta completada por separado. Cuando se considera una nueva entrada, escanea
hasta `TotalTrigger` operaciones anteriores de esa dirección. Si al menos `LossTrigger` operaciones dentro de esa ventana terminaron con un resultado
negativo, se usa el volumen reducido; en caso contrario, se opera el volumen normal.

## Notas

* La lógica de stop-loss y take-profit basada en pasos de precio depende del valor `Security.PriceStep`. Si el instrumento no lo proporciona,
  se asume un paso de `1`.
* El filtro FATL necesita al menos 39 velas históricas antes de ser operativo. No se generan operaciones hasta que se acumulen suficientes datos.
* La estrategia mantiene un historial compacto de operaciones para el bloque MMRecounter; una vez que el historial supera los 100 elementos, los registros más antiguos
  se descartan automáticamente.
