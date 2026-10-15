# Estrategia ColorMaRsi Trigger MMRec Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port de la API de alto nivel de StockSharp del experto de MetaTrader **Exp_ColorMaRsi-Trigger_MMRec_Duplex.mq5**. Ejecuta dos bloques MaRsi-Trigger independientes: uno para oportunidades largas y otro para oportunidades cortas. Cada bloque evalúa una señal compuesta generada comparando una media móvil rápida y lenta junto con un RSI rápido y lento. El valor compuesto se limita al rango `[-1, 1]`, reproduciendo el comportamiento del indicador original: `+1` marca alineación alcista, `-1` marca alineación bajista, y `0` indica condiciones mixtas.

Un módulo de gestión de capital "MMRec" monitorea las últimas operaciones para cada dirección. Cuando un número configurable de pérdidas aparece dentro de una ventana móvil, la siguiente operación cambia a un volumen reducido hasta que el rendimiento se recupere. Esto reproduce la lógica de dimensionamiento de posición adaptativo de la biblioteca de MetaTrader `TradeAlgorithms.mqh` usada por el experto.

## Lógica de trading

1. **Pipeline del indicador** (por bloque):
   - Calcular una media móvil rápida (`MA_fast`) y una lenta (`MA_slow`) en el precio aplicado y marco temporal seleccionados.
   - Calcular un RSI rápido (`RSI_fast`) y uno lento (`RSI_slow`) en posiblemente diferentes precios aplicados.
   - Construir una puntuación de color: empezar en `0`, añadir `+1` si `MA_fast > MA_slow` o `-1` en caso contrario, luego añadir `+1` si `RSI_fast > RSI_slow` o `-1` en caso contrario. Limitar el resultado a `[-1, 1]`.
   - Almacenar el historial de puntuaciones y leerlo con el desplazamiento `SignalBar` configurado (el valor por defecto coincide con la implementación de MetaTrader).

2. **Bloque largo**:
   - **Entrada**: permitida cuando no hay posición larga abierta (los cortos se cubren primero). El color anterior (`SignalBar + 1`) debe ser `+1` mientras el color actual (`SignalBar`) es `≤ 0`, indicando que el bloque alcista acaba de neutralizarse.
   - **Salida**: cuando el color anterior se vuelve negativo (`-1`) y las salidas están habilitadas.

3. **Bloque corto**:
   - **Entrada**: permitida cuando no hay posición corta abierta (los largos se cierran primero). El color anterior debe ser `-1` mientras el color actual es `≥ 0`, señalando una transición fresca de bajista a neutral.
   - **Salida**: cuando el color anterior se vuelve positivo y las salidas están habilitadas.

4. **Stops y objetivos**: las distancias opcionales de stop-loss y take-profit se expresan en pasos de precio y se re-evalúan en cada vela terminada. Cruzar cualquier límite cierra el respectivo posición inmediatamente.

5. **Gestión de capital**: la estrategia almacena el resultado de cada operación completada (por dirección) y cuenta el número de pérdidas en las últimas `HistoryDepth` operaciones. Si el recuento de pérdidas alcanza `LossTrigger`, la siguiente orden usa el volumen reducido. De lo contrario, se usa el volumen normal.

## Parámetros

| Grupo | Nombre | Descripción | Por defecto |
| --- | --- | --- | --- |
| Bloque Largo | `LongCandleType` | Marco temporal que alimenta el bloque MaRsi-Trigger largo. | `H4` |
|  | `LongAllowOpen` / `LongAllowClose` | Habilitar apertura / cierre de posiciones largas. | `true` |
|  | `LongStopLossPoints` / `LongTakeProfitPoints` | Distancias protectoras en puntos del instrumento. Establecer en `0` para deshabilitar. | `1000` / `2000` |
|  | `LongSignalBar` | Número de barras completadas para desplazar al muestrear los buffers del indicador. | `1` |
|  | `LongRsiPeriod` / `LongRsiLongPeriod` | Longitudes de RSI rápido y lento. | `3` / `13` |
|  | `LongMaPeriod` / `LongMaLongPeriod` | Longitudes de media móvil rápida y lenta. | `5` / `10` |
|  | `LongRsiPrice` / `LongRsiLongPrice` | Precio aplicado para RSI rápido / lento (Close, Open, High, Low, Median, Typical, Weighted). | `Weighted` / `Median` |
|  | `LongMaPrice` / `LongMaLongPrice` | Precio aplicado para MA rápida / lenta. | `Close` / `Close` |
|  | `LongMaType` / `LongMaLongType` | Algoritmos de media móvil (Simple, Exponential, Smoothed, Weighted). | `Exponential` / `Exponential` |
| Gestión de Capital | `LongNormalVolume` / `LongReducedVolume` | Volumen de operación largo estándar y reducido. | `0.1` / `0.01` |
|  | `LongHistoryDepth` | Número de operaciones largas recientes observadas por el filtro de gestión de capital. | `5` |
|  | `LongLossTrigger` | Recuento mínimo de pérdidas dentro de la ventana para cambiar al volumen largo reducido. | `3` |

| Grupo | Nombre | Descripción | Por defecto |
| --- | --- | --- | --- |
| Bloque Corto | `ShortCandleType` | Marco temporal que alimenta el bloque MaRsi-Trigger corto. | `H4` |
|  | `ShortAllowOpen` / `ShortAllowClose` | Habilitar apertura / cierre de posiciones cortas. | `true` |
|  | `ShortStopLossPoints` / `ShortTakeProfitPoints` | Distancias protectoras en puntos del instrumento. Establecer en `0` para deshabilitar. | `1000` / `2000` |
|  | `ShortSignalBar` | Número de barras completadas para desplazar al muestrear los buffers del indicador. | `1` |
|  | `ShortRsiPeriod` / `ShortRsiLongPeriod` | Longitudes de RSI rápido y lento. | `3` / `13` |
|  | `ShortMaPeriod` / `ShortMaLongPeriod` | Longitudes de media móvil rápida y lenta. | `5` / `10` |
|  | `ShortRsiPrice` / `ShortRsiLongPrice` | Precio aplicado para RSI rápido / lento. | `Weighted` / `Median` |
|  | `ShortMaPrice` / `ShortMaLongPrice` | Precio aplicado para MA rápida / lenta. | `Close` / `Close` |
|  | `ShortMaType` / `ShortMaLongType` | Algoritmos de media móvil (Simple, Exponential, Smoothed, Weighted). | `Exponential` / `Exponential` |
| Gestión de Capital | `ShortNormalVolume` / `ShortReducedVolume` | Volumen de operación corto estándar y reducido. | `0.1` / `0.01` |
|  | `ShortHistoryDepth` | Número de operaciones cortas recientes observadas por el filtro de gestión de capital. | `5` |
|  | `ShortLossTrigger` | Recuento mínimo de pérdidas dentro de la ventana para cambiar al volumen corto reducido. | `3` |

## Notas

- Las opciones de precio aplicado siguen la semántica de MetaTrader. Por ejemplo, `Weighted` equivale a `(High + Low + 2 * Close) / 4` y `Typical` equivale a `(High + Low + Close) / 3`.
- Cuando los bloques largo y corto comparten el mismo marco temporal (por defecto), una sola suscripción de velas alimenta ambas calculadoras.
- Establecer el disparador de pérdidas en `0` fuerza el volumen reducido inmediatamente, imitando el comportamiento del helper original de gestión de capital.
- La estrategia usa órdenes de mercado; el parámetro `Deviation` de MetaTrader por lo tanto no es necesario.
