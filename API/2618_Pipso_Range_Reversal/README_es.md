# Estrategia de Reversión de Rango Pipso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de StockSharp del asesor experto Pipso de MQL5. Actúa como un sistema de reversión a la media que vende en rupturas alcistas y compra en rupturas bajistas de un rango reciente de máximo/mínimo, limitando la actividad a una sesión de trading configurable.

## Idea principal
- Construir un canal estilo Donchian a partir del máximo más alto y el mínimo más bajo de los últimos `LookbackPeriod` candles finalizados (por defecto 36).
- Monitorear el límite superior para desvanecer rupturas al alza y el límite inferior para desvanecer rupturas a la baja.
- Abrir posiciones solo cuando el candle actual comienza dentro de la ventana de trading definida por `StartHour` y `EndHour`.

## Lógica de trading
### Criterios de entrada
- **Entrada corta**: cuando el máximo del candle toca o supera el máximo del canal anterior, cerrar cualquier posición larga y, si se está dentro de la ventana de sesión, vender `OrderVolume` contratos a mercado. El modelo registra el precio de entrada como el máximo del canal.
- **Entrada larga**: cuando el mínimo del candle toca o rompe por debajo del mínimo del canal anterior, cerrar cualquier posición corta y, si el trading está permitido, comprar `OrderVolume` contratos a mercado con el mínimo del canal como referencia de entrada.

### Criterios de salida
- Las posiciones se cierran inmediatamente cuando el precio toca el lado opuesto del canal (reflejando el comportamiento del EA original).
- Se coloca un stop de protección a una distancia fija del precio de entrada. La distancia del stop equivale a `(channelHigh - channelLow) * (1 + StopRangePercent / 100)`; con el valor predeterminado `StopRangePercent = 300` el stop queda a cuatro anchos de canal de distancia.
- Los stops se evalúan en los extremos del candle: una posición larga se cierra si el mínimo del candle cae por debajo del stop, y una corta si el máximo supera el stop.

### Filtro de sesión
- `StartHour` y `EndHour` se especifican en hora del exchange. Si `StartHour < EndHour` la estrategia opera solo entre esas horas en el mismo día. Si `StartHour > EndHour` la ventana cruza la medianoche, habilitando sesiones nocturnas (por ej., 21 → 9).
- Cuando la ventana está deshabilitada (`StartHour == EndHour`) la estrategia permanece sin posiciones.

## Parámetros
- **OrderVolume** *(predeterminado 0.1)* – volumen de trading por orden.
- **LookbackPeriod** *(predeterminado 36)* – número de candles usados para calcular el canal.
- **StartHour** *(predeterminado 21)* – hora (0–23) en que se abre la sesión.
- **EndHour** *(predeterminado 9)* – hora (0–23) en que se cierra la sesión.
- **StopRangePercent** *(predeterminado 300)* – porcentaje adicional del ancho del canal añadido al rango bruto antes de convertirlo a distancia de stop.
- **CandleType** *(predeterminado candles de 1 hora)* – marco temporal usado para los cálculos.

## Indicadores y datos
- Usa los indicadores `Highest` y `Lowest` de StockSharp para rastrear los límites del canal.
- Funciona con cualquier instrumento que proporcione datos de candles continuos que coincidan con el `CandleType` seleccionado.
- El EA original espera que el marco temporal del gráfico represente el horizonte de decisión; puede ajustar `CandleType` para reproducir esas condiciones.

## Notas
- La lógica opera en candles finalizados para evitar ruido intrabarra; en feeds en vivo los precios de stop/entrada aproximan dónde el EA de MQL5 interactuaría con los ticks.
- No se define un objetivo de take-profit—los beneficios se realizan cuando el precio revierte al límite opuesto o cuando se alcanza el stop.
- Considere calibrar las horas de sesión, la longitud del rango y el multiplicador del stop a la volatilidad del instrumento de trading.
