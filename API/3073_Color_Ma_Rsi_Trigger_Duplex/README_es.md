# Estrategia Color Ma RSI Trigger Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto **Exp_ColorMaRsi-Trigger_Duplex.mq5** a la API de alto nivel de StockSharp.
Maneja dos detectores MaRsi-Trigger independientes: el **bloque largo** decide cuándo se deben abrir o cerrar posiciones largas, mientras que el **bloque corto** realiza la misma tarea para posiciones cortas. Cada detector evalúa si un indicador personalizado reporta presión de mercado alcista (`+1`), neutral (`0`) o bajista (`-1`). La lógica original de MetaTrader se conserva, incluyendo la confirmación retardada que espera dos barras completadas antes de reaccionar y la configuración de gestión de dinero separada por dirección.

## Idea de trading

1. Calcular dos medias móviles exponenciales (rápida y lenta) y dos osciladores RSI (rápido y lento) en una serie de velas seleccionable para cada bloque.
2. En cada vela finalizada el indicador devuelve `+1` cuando ambos estudios rápidos dominan a sus contrapartes lentas, `-1` cuando ambos son más débiles y `0` en caso contrario. El valor bruto se limita al rango `[-1, 1]` como en el indicador MT5.
3. La estrategia almacena un historial continuo de valores del indicador. Para un desplazamiento `SignalBar` configurado, compara el valor de la barra `SignalBar + 1` períodos atrás (llamado `older`) con el valor de la barra `SignalBar` períodos atrás (llamado `recent`).
4. Lógica larga:
   - Si `older < 0` el bloque largo cierra cualquier posición larga activa (siempre que las salidas largas estén habilitadas).
   - Si `older > 0` **y** `recent <= 0` el bloque largo prepara una nueva entrada larga (siempre que las entradas largas estén habilitadas).
5. La lógica corta refleja el bloque largo:
   - Si `older > 0` el bloque corto sale de posiciones cortas existentes (cuando las salidas cortas están habilitadas).
   - Si `older < 0` **y** `recent >= 0` el bloque abre una nueva posición corta (cuando las entradas cortas están habilitadas).
6. Niveles opcionales de stop-loss y take-profit, expresados en pasos de precio del instrumento, cierran posiciones cuando el precio cruza los niveles configurados.

Los dos bloques pueden suscribirse a diferentes marcos temporales de velas y fuentes de precio, permitiendo al usuario replicar el comportamiento dual de marcos temporales original o experimentar con combinaciones alternativas.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `LongCandleType`, `ShortCandleType` | Series de datos de velas usadas por los bloques largo y corto. Por defecto velas de 4 horas. |
| `LongVolume`, `ShortVolume` | Volumen de mercado negociado cuando el bloque correspondiente abre una nueva posición. |
| `LongAllowOpen`, `ShortAllowOpen` | Habilitar o deshabilitar la apertura de nuevas posiciones para cada bloque. |
| `LongAllowClose`, `ShortAllowClose` | Habilitar o deshabilitar las señales de cierre para cada bloque. |
| `LongStopLossPoints`, `ShortStopLossPoints` | Distancia de stop-loss medida en pasos de precio. Establecer en `0` para deshabilitar. |
| `LongTakeProfitPoints`, `ShortTakeProfitPoints` | Distancia de take-profit medida en pasos de precio. Establecer en `0` para deshabilitar. |
| `LongSignalBar`, `ShortSignalBar` | Número de barras completadas entre la vela actual y la usada para la lógica de decisión. |
| `LongRsiPeriod`, `LongRsiLongPeriod`, `ShortRsiPeriod`, `ShortRsiLongPeriod` | Longitudes de los osciladores RSI rápido y lento. |
| `LongMaPeriod`, `LongMaLongPeriod`, `ShortMaPeriod`, `ShortMaLongPeriod` | Longitudes de las medias móviles rápida y lenta. |
| `LongRsiPrice`, `ShortRsiPrice` | Fuente de precio alimentada al RSI rápido (cierre, apertura, máximo, mínimo, mediana, típico o ponderado). |
| `LongRsiLongPrice`, `ShortRsiLongPrice` | Fuente de precio alimentada al RSI lento. |
| `LongMaPrice`, `ShortMaPrice` | Fuente de precio alimentada a la media móvil rápida. |
| `LongMaLongPrice`, `ShortMaLongPrice` | Fuente de precio alimentada a la media móvil lenta. |
| `LongMaType`, `ShortMaType` | Método de media móvil para la línea rápida (simple, exponencial, suavizada o ponderada). |
| `LongMaLongType`, `ShortMaLongType` | Método de media móvil para la línea lenta. |

## Reglas de trading

1. Esperar hasta que la serie de velas seleccionada produzca barras terminadas y todos los indicadores estén completamente calentados.
2. Para cada bloque calcular el valor MaRsi-Trigger y actualizar el búfer de historial.
3. Cuando el historial contiene al menos `SignalBar + 2` entradas, evaluar las condiciones largas y cortas descritas en la sección de idea de trading.
4. Antes de abrir una posición la estrategia neutralizará cualquier exposición opuesta (si la bandera de cierre correspondiente está habilitada). Por ejemplo, una nueva entrada larga comprará suficiente volumen para cerrar una posición corta y solo entonces añadirá el volumen largo.
5. Después de que se abre una posición, los niveles opcionales de stop-loss y take-profit se monitorean en cada vela finalizada.
6. Las órdenes de apertura y cierre se envían como órdenes de mercado a través de los helpers de alto nivel `BuyMarket` y `SellMarket`.

## Gestión de riesgo

* Los stops y objetivos se miden usando `Security.PriceStep`. Cuando el instrumento no expone un paso de precio, se asume un valor predeterminado de `1`, coincidiendo con el comportamiento de muchas estrategias existentes en este repositorio.
* Los bloques largo y corto mantienen configuraciones independientes de stop y take.
* La estrategia no coloca órdenes protectoras adicionales (como stops de seguimiento); el comportamiento refleja el experto MT5, que solo cierra operaciones cuando el indicador se activa o cuando se alcanza el stop/objetivo duro.

## Notas

* El port de StockSharp emite órdenes de mercado inmediatamente después de que la vela de evaluación finaliza. En MetaTrader el experto programaba sus órdenes para el tiempo de apertura de la siguiente barra mediante desplazamientos de marca de tiempo; ambos comportamientos se alinean efectivamente porque StockSharp procesa la señal en cuanto el vela se cierra.
* El EA original exponía varios modos de gestión de dinero (`LOT`, `BALANCE`, etc.). Las estrategias de StockSharp trabajan con valores de volumen directos, por lo tanto el port mantiene el volumen como parámetro directo (`LongVolume`/`ShortVolume`).
* El slippage y la lógica específica del número mágico de la librería auxiliar MT5 no son necesarios en StockSharp y han sido omitidos.
* Los cálculos del indicador aprovechan las implementaciones integradas de StockSharp de medias móviles y RSI; la salida se limita a `[-1, 1]` para coincidir con el indicador original `ColorMaRsi-Trigger`.
