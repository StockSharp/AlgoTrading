# Estrategia de Promediado de Posición MA MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión fiel del asesor experto de MetaTrader **"MA MACD Position averaging"**. Combina un filtro de media
móvil ponderada con una verificación del ratio MACD y añade un módulo de promediado al estilo martingala que aumenta el tamaño de
la posición cada vez que el precio se mueve adversamente en un número configurable de pips. Todos los parámetros de riesgo se
configuran en unidades de pips y se convierten internamente a desplazamientos de precio utilizando los metadatos del instrumento
proporcionados por StockSharp.

## Lógica de Trading

1. **Preparación de indicadores**
   - Una media móvil configurable (`MaPeriod`, `MaMethod`, `MaAppliedPrice`) se muestrea en velas completadas. Los parámetros
     `SignalBar` y `MaShift` emulan la capacidad de MetaTrader para mirar hacia atrás un número dado de barras y trazar la media
     móvil con un desplazamiento horizontal.
   - Un indicador MACD (`MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod`, `MacdAppliedPrice`) se procesa en las mismas
     velas. La estrategia almacena las líneas principal y de señal del MACD en un pequeño búfer circular para que se pueda acceder
     a valores históricos sin llamar directamente a las APIs de indicadores.
2. **Condiciones de entrada**
   - **Largo**: ambas líneas MACD están por debajo de cero, el ratio `MACDmain / MACDsignal` es mayor o igual a `MacdRatio`,
     el cierre de la vela está por encima de la media móvil muestreada y la distancia entre el precio y la media es al menos
     `IndentPips` pips.
   - **Corto**: ambas líneas MACD están por encima de cero, el ratio está por encima de `MacdRatio`, el cierre de la vela está
     por debajo de la media móvil y la distancia entre ellas es al menos `IndentPips` pips.
   - Las nuevas entradas solo se permiten cuando la estrategia no tiene exposición. Cuando ya hay un ciclo de promediado en
     progreso, la lógica de señal se omite y solo se aplican las reglas de promediado.
3. **Módulo de promediado**
   - Cuando existe una posición larga y el precio baja al menos `StepLossingPips` desde la mejor entrada larga (la más baja),
     la estrategia abre una operación larga adicional cuyo volumen es igual al volumen del último tramo multiplicado por
     `LotCoefficient` (redondeado al paso de volumen del instrumento).
   - Cuando existe una posición corta y el precio sube al menos `StepLossingPips` desde la mejor entrada corta (la más alta),
     se añade un nuevo tramo corto usando el mismo multiplicador `LotCoefficient`.
   - Si se detecta exposición en ambas direcciones (nunca debería ocurrir en condiciones normales), la estrategia cierra
     inmediatamente todos los tramos para restaurar la consistencia.
4. **Salidas protectoras**
   - Cada tramo almacena niveles individuales de stop-loss y take-profit expresados en unidades de precio (`StopLossPips`,
     `TakeProfitPips`). En cada vela terminada, la estrategia verifica si el rango de la vela cruzó alguno de los niveles
     almacenados y, si es así, cierra el tramo con una orden de mercado.
   - Un trailing stop (`TrailingStopPips`, `TrailingStepPips`) es opcional. Una vez que el precio avanza en favor de un tramo
     en `TrailingStopPips + TrailingStepPips`, el stop se mueve a `TrailingStopPips` pips detrás del cierre actual. El stop
     solo se ajusta si el precio hace un progreso adicional de al menos `TrailingStepPips` pips.
5. **Mantenimiento**
   - Los comandos de volumen se alinean al paso de volumen del instrumento y se recortan al mínimo/máximo permitido. La
     estrategia ejecuta solo en velas completamente formadas (`CandleStates.Finished`) para evitar el doble procesamiento.

## Parámetros

| Parámetro | Tipo | Valor predeterminado | Descripción |
|-----------|------|----------------------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Marco temporal usado para los cálculos de indicadores. |
| `OrderVolume` | `decimal` | `0.1` | Tamaño de lote base para la entrada inicial. |
| `StopLossPips` | `int` | `50` | Distancia del stop-loss en pips (0 deshabilita el stop). |
| `TakeProfitPips` | `int` | `50` | Distancia del take-profit en pips (0 deshabilita el objetivo). |
| `TrailingStopPips` | `int` | `5` | Desplazamiento del trailing stop en pips. Debe ser positivo para habilitar el trailing. |
| `TrailingStepPips` | `int` | `5` | Distancia pip extra requerida antes de que el trailing stop se mueva de nuevo. |
| `StepLossingPips` | `int` | `30` | Retroceso de precio en pips que activa un nuevo tramo de promediado. |
| `LotCoefficient` | `decimal` | `2.0` | Multiplicador aplicado al volumen del tramo anterior al promediar. |
| `SignalBar` | `int` | `0` | Número de barras completadas para mirar atrás al muestrear indicadores. |
| `MaPeriod` | `int` | `15` | Longitud de la media móvil en barras. |
| `MaShift` | `int` | `0` | Desplazamiento horizontal (en barras) aplicado a los valores de la media móvil. |
| `MaMethod` | `MovingAverageMethod` | `Weighted` | Algoritmo de suavizado de la media móvil (simple, exponencial, suavizado, ponderado). |
| `MaAppliedPrice` | `AppliedPriceType` | `Weighted` | Precio de la vela usado como entrada para la media móvil. |
| `IndentPips` | `int` | `4` | Brecha mínima en pips requerida entre el precio y la media móvil antes de entrar. |
| `MacdFastPeriod` | `int` | `12` | Longitud de EMA rápida del filtro MACD. |
| `MacdSlowPeriod` | `int` | `26` | Longitud de EMA lenta del filtro MACD. |
| `MacdSignalPeriod` | `int` | `9` | Longitud de la línea de señal del filtro MACD. |
| `MacdAppliedPrice` | `AppliedPriceType` | `Weighted` | Precio aplicado usado para el cálculo del MACD. |
| `MacdRatio` | `decimal` | `0.9` | Ratio mínimo MACD principal/señal requerido para permitir trading. |

### Conversión de pips

Todos los ajustes basados en pips (`StopLossPips`, `TakeProfitPips`, `TrailingStopPips`, `TrailingStepPips`, `StepLossingPips`,
`IndentPips`) se multiplican por el `PriceStep` del instrumento. Cuando el instrumento tiene 3 o 5 decimales, el valor se
multiplica adicionalmente por 10 para reproducir la definición de "pip" de MetaTrader para cotizaciones fraccionales. Si no
hay paso de precio disponible, se usa un valor de respaldo de `0.0001`.

## Notas de Implementación

- La estrategia mantiene una lista interna de tramos de posición porque StockSharp opera en modo netting. Cada tramo rastrea
  su propio precio de entrada, stop y niveles de take para que el promediado se comporte como el EA original de MetaTrader.
- Las órdenes protectoras se simulan en software: cuando una vela toca un nivel de stop-loss o take-profit, la posición se
  cierra con una orden de mercado en esa barra.
- El promediado se deshabilita automáticamente cuando `StepLossingPips` es cero. De lo contrario, cada tramo adicional usa el
  volumen del tramo anterior multiplicado por `LotCoefficient` y redondeado hacia abajo al paso de volumen más cercano.
- Las actualizaciones del trailing stop usan el cierre de la vela como proxy del precio actual. El stop nunca se mueve en la
  dirección adversa y permanece inactivo hasta que el progreso del precio excede `TrailingStopPips + TrailingStepPips`.
- Los búferes de indicadores respetan los desplazamientos `SignalBar` y `MaShift` para que la lógica de decisión vea
  exactamente los mismos valores que el experto de MetaTrader obtendría de sus búferes de indicadores.
