# Básico Martingale EA 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **Básica Martingale EA 3** replica el asesor experto MetaTrader 5 que combina un filtro de tendencias basado en la media móvil exponencial triple (TEMA) con un promedio de martingala impulsado por ATR. La versión convertida StockSharp mantiene los mismos parámetros de riesgo, ventana de negociación y lógica de administración de dinero, al tiempo que expone todo a través de parámetros de estrategia para su optimización.

## Lógica comercial
1. **Generación de señal**: en cada vela completa del período de tiempo seleccionado, el precio de cierre se compara con el valor de TEMA. Un cierre por encima del indicador abre una cesta larga, mientras que un cierre por debajo abre una cesta corta. Sólo una dirección puede estar activa al mismo tiempo.
2. **Ventana de negociación**: se permiten cestas nuevas solo entre el `StartHour` y el `EndHour` (hora de intercambio). Si ambos horarios son iguales la ventana se considera siempre abierta. Establezca `TradeAtNewBar` en `true` para limitar las cestas nuevas a una por vela, similar al cambio original `TradeAtNewBar` en MT5.
3. **Cuadrícula promedio**: una vez que existe una posición, la estrategia mide la distancia desde el peor/mejor precio de entrada. Siempre que el mercado se mueve al menos `GridMultiplier × ATR`, se agrega una orden adicional en la dirección definida por `Averaging` (promedio hacia abajo o promedio hacia arriba) hasta alcanzar `MaxAverageOrders`. El nuevo tamaño del pedido sigue el modo martingala elegido (`Multiply` o `Increment`).
4. **Salidas protectoras**: los niveles opcionales de stop-loss y take-profit se heredan de la primera orden de la cesta. Además, el bloque final imita la implementación de MT5: después de `TrailingStart` puntos de ganancia, el stop se mueve a `price - TrailingStop` (o `price + TrailingStop` para cortos) y se ajusta en `TrailingStep`.
5. **Aplanamiento**: si se toca algún nivel stop, take-profit o trailing, toda la cesta se cierra en el mercado y todos los contadores de promedio se reinician.

## Parámetros
| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | Periodo H1 | Serie de velas que impulsa la estrategia. |
| `StartVolume` | `decimal` | `0.01` | Volumen inicial para el primer pedido en una cesta. |
| `StopLossPoints` | `decimal` | `20` | Distancia de stop-loss en pasos de precio. Establezca en `0` para desactivar. |
| `TakeProfitPoints` | `decimal` | `20` | Distancia de obtención de beneficios en pasos de precio. Establezca en `0` para desactivar. |
| `StartHour` | `int` | `3` | Hora (inclusive) en la que se pueden iniciar nuevas cestas. |
| `EndHour` | `int` | `18` | Hora (exclusiva) en la que se detiene la creación de la cesta. |
| `TemaPeriod` | `int` | `50` | Longitud del indicador TEMA. |
| `BarsCalculated` | `int` | `3` | Número de velas terminadas necesarias antes de que comience la negociación. |
| `AtrPeriod` | `int` | `14` | Período del indicador Average True Range. |
| `GridMultiplier` | `decimal` | `0.75` | multiplicador ATR que define el espaciado de la cuadrícula. |
| `MaxAverageOrders` | `int` | `3` | Número máximo de órdenes promediadas por dirección (incluida la inicial). |
| `Averaging` | enumeración | `AverageDown` | Elija entre promediar en reducción, promediar en ganancias o deshabilitar entradas adicionales. |
| `Martin` | enumeración | `Multiply` | Seleccione entre tamaño de martingala multiplicativo o incremental. |
| `LotMultiplier` | `decimal` | `1.5` | Factor utilizado por el modo martingala `Multiply`. |
| `LotIncrement` | `decimal` | `0.1` | Volumen adicional utilizado por el modo martingala `Increment`. |
| `TradeAtNewBar` | `bool` | `false` | Limite las cestas nuevas a una por cada vela terminada. |
| `TrailingStart` | `int` | `100` | Ganancia en puntos necesarios para activar el seguimiento. |
| `TrailingStop` | `int` | `50` | Distancia del trailing stop en puntos. |
| `TrailingStep` | `int` | `30` | Mejora mínima (puntos) antes de volver a mover el trailing stop. |

## Notas de conversión
- La versión StockSharp mantiene la configuración del indicador MT5 (TEMA(50) + ATR(14)) y expone el parámetro `bar` como `BarsCalculated`, asegurando al menos el número especificado de velas antes de operar.
- El manejo del volumen respeta los `MinVolume`, `MaxVolume` y `VolumeStep` del instrumento, por lo que las operaciones en vivo respetan los límites de intercambio incluso con pasos fraccionarios de martingala.
- La lógica de seguimiento sigue el comportamiento original de equilibrio más el paso de seguimiento, pero se implementa con datos de posición agregados porque las posiciones StockSharp se compensan por instrumento.
- Las anotaciones de gráficos del experto MT5 no se transfirieron porque StockSharp ya proporciona visualización de orden y posición en los paneles de gráficos.
