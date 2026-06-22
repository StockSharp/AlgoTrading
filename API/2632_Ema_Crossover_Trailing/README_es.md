# Estrategia EMA Crossover con Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port de StockSharp del asesor experto MQL5 **"Intersection 2 iMA"**. Opera en dos medias móviles exponenciales (EMAs) y reacciona a los cruces que ocurren en velas completamente formadas. El experto original fue diseñado para MetaTrader 5 y gestionaba el volumen de operaciones de forma dinámica; en esta conversión el tamaño de la orden está controlado por un parámetro configurable mientras se preserva la lógica de cruce y trailing.

## Lógica de trading
1. **Generación de señales**
   - Calcular las EMAs rápida y lenta en la serie de velas seleccionada.
   - Un **cruce alcista** (EMA rápida cruzando por encima de la EMA lenta) dispara una señal de compra cuando la vela anterior cerró con la EMA rápida por debajo o igual a la EMA lenta y los valores actuales muestran la EMA rápida por encima de la EMA lenta.
   - Un **cruce bajista** (EMA rápida cruzando por debajo de la EMA lenta) espeja la regla anterior y produce una señal de venta.
2. **Ejecución de órdenes**
   - Cuando se produce una señal de compra y no existe posición larga, la estrategia envía una orden de compra a mercado.
   - Cuando se produce una señal de venta y no existe posición corta, la estrategia envía una orden de venta a mercado.
   - Si hay una posición opuesta, el volumen de la orden se incrementa para cerrar la posición existente antes de establecer la nueva, coincidiendo con el comportamiento del EA fuente que primero cerraba operaciones opuestas.
3. **Gestión del trailing stop**
   - Un trailing stop escalonado mantiene una distancia fija (en pasos de precio) del precio más favorable.
   - El stop solo se mueve cuando el precio ha avanzado un paso definido por el usuario, previniendo modificaciones constantes de órdenes.
   - Si el precio viola el nivel del trailing, la posición se cierra con una orden de mercado.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `FastPeriod` | Longitud de la EMA rápida. | 4 |
| `SlowPeriod` | Longitud de la EMA lenta. | 18 |
| `TrailingStopPoints` | Distancia entre precio de mercado y trailing stop en pasos de precio (puntos). Un valor de `0` deshabilita el trailing. | 20 |
| `TrailingStepPoints` | Progreso mínimo en pasos de precio antes de que el trailing stop avance. | 5 |
| `CandleType` | Serie de datos de velas usada para los cálculos (marco temporal). | Velas de 15 minutos |
| `TradeVolume` | Tamaño de la orden para entradas a mercado. | 1 |

## Notas de implementación
- La estrategia usa la API de alto nivel `SubscribeCandles().Bind(...)` para conectar datos de velas con indicadores EMA, asegurando que no se necesite gestión manual de buffers.
- Las distancias de trailing se calculan multiplicando el número configurado de puntos por el `PriceStep` del instrumento, replicando la lógica de ajuste de dígitos encontrada en la versión MQL.
- Los trailing stops se implementan internamente usando salidas a mercado, porque StockSharp no expone el mismo helper `PositionModify` usado en MetaTrader. El comportamiento sigue siendo equivalente: una vez que se viola el nivel del trailing, la posición se sale inmediatamente.
- Los parámetros se exponen a través de `StrategyParam<T>` para que puedan optimizarse en el diseñador o ajustarse desde la UI.

## Consejos de uso
- Alinear el `CandleType` con el marco temporal usado en backtests o trading en vivo para mantener los valores del indicador consistentes.
- Al operar instrumentos con tamaños de tick pequeños, ajustar `TrailingStopPoints` y `TrailingStepPoints` correspondientemente; la distancia de precio efectiva equivale a *puntos × PriceStep*.
- Establecer `TradeVolume` para que coincida con el contrato o tamaño de lote deseado. La estrategia incrementa automáticamente el importe de la orden para cerrar una posición opuesta cuando aparece una nueva señal.

## Diferencias respecto al Asesor Experto original
- La gestión de capital en MetaTrader usaba `MoneyFixedMargin`; la versión de StockSharp expone un parámetro de volumen de orden fijo en su lugar, dejando el dimensionamiento avanzado de posiciones a la configuración externa.
- El EA ofrecía una entrada `InpCloseHalf` no utilizada. No tenía efecto en el código fuente y fue omitida.
- El trailing stop se gestiona internamente en lugar de modificar órdenes de stop-loss, ya que esto simplifica la ejecución dentro de StockSharp manteniendo la lógica de salida idéntica.
