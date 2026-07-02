# Mi trailing stop de media móvil TS15
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia reproduce el comportamiento del asesor experto original **my_ts15.mq5** al gestionar órdenes de trailing stop en torno a una posición neta existente. Un promedio móvil ponderado lineal (LWMA) impulsa la colocación de las paradas y puede ser reemplazado por otros métodos de suavizado. La lógica continuamente:

* Lee el valor promedio móvil de un número configurable de velas completadas.
* Compara el progreso de los precios con la trayectoria del promedio móvil y las compensaciones basadas en precios.
* Mueve la orden de parada de protección solo cuando el nuevo nivel mejora el anterior al menos en el paso especificado.
* Opcionalmente, impone una distancia máxima de pérdida fijando el tope o liquidando inmediatamente la posición cuando se rompe el límite.

The strategy does not produce entry signals. It is meant to run together with other components (manual or automated) that open positions on the same security.

## Lógica de trading

1. Suscríbase a la serie de velas seleccionada y vincule un indicador de promedio móvil usando la API de alto nivel de StockSharp.
2. Tan pronto como finalice una vela, almacene el resultado del indicador y obtenga el valor que está `MaBarsTrail + MaShift` barras detrás de la barra actual.
3. Convierta la configuración basada en puntos a distancias de precios absolutas utilizando el tamaño de tick del instrumento.
4. Para posiciones largas, elija la más baja de:
   * The moving average minus its offset.
   * El precio actual menos la compensación "en ganancias".
Luego fije el sendero a la distancia "en pérdida" y, opcionalmente, a la pérdida máxima permitida.
5. Para posiciones cortas, elija la más alta de:
   * The moving average plus its offset.
   * El precio actual más la compensación "en ganancias".
Luego fije el sendero a la distancia "en pérdida" y, opcionalmente, a la pérdida máxima permitida.
6. Actualice la orden de detención solo cuando la mejora exceda `TrailStepPoints` (a menos que sea cero, en cuyo caso se aceptan todas las mejoras).
7. If the price breaches the maximum loss distance and `EnforceMaxStopLoss` is enabled, the strategy closes the position immediately.

Todas las entradas de precios utilizan el precio de vela especificado en `MaPrice`, que coincide con la configuración original de MQL donde el indicador se alimenta con la serie `PRICE_WEIGHTED`.

## Parámetros

| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `MaPeriod` | `50` | Longitud de la media móvil utilizada como columna vertebral final. |
| `MaShift` | `0` | Desplazamiento adicional (en barras) aplicado al muestrear el valor de la media móvil. |
| `MaMethod` | `LinearWeighted` | Método de suavizado de la media móvil (simple, exponencial, suavizado, ponderado lineal). |
| `MaPrice` | `Weighted` | Precio de vela alimentado a la media móvil. |
| `MaBarsTrail` | `1` | Número de barras completadas entre la vela actual y la muestra de media móvil. |
| `TrailBehindMaPoints` | `5` | Distancia en puntos mantenida entre el stop y la media móvil. |
| `TrailBehindPricePoints` | `30` | Distancia en puntos que se mantiene detrás del precio cuando la posición es rentable. |
| `TrailBehindNegativePoints` | `60` | Distancia en puntos que se mantiene detrás del precio cuando la posición está perdiendo. |
| `TrailStepPoints` | `0` | Mejora mínima (en puntos) requerida antes de mover la parada. Zero replica el comportamiento de “actualizar siempre”. |
| `EnforceMaxStopLoss` | `false` | Si está habilitado, limite el stop a la pérdida máxima permitida y liquide la posición cuando el precio supere ese límite. |
| `MaxStopLossPoints` | `100` | Maximum allowed loss distance in points. |
| `ShowIndicator` | `true` | Dibuje la media móvil y los marcadores comerciales en el gráfico cuando la interfaz de usuario esté disponible. |
| `CandleType` | `M1` | Tipo de datos de vela que impulsa los cálculos. |

Todas las entradas basadas en puntos se convierten a distancias de precios mediante el tamaño del pip del instrumento calculado a partir de `Security.PriceStep`.

## Notas de conversión

* El experto MQL actualizó el identificador MA manualmente. La implementación StockSharp utiliza `BindEx` para procesar el indicador sin acceder a los buffers internos ni llamar a `GetValue`.
* Bid/Ask prices are not directly available from finished candles, therefore the trailing calculations use the candle price selected by `MaPrice`. This keeps the behaviour consistent because the original script fed the indicator with the same weighted price and compared it with Bid/Ask ticks.
* `PositionModify` se reemplaza cancelando y recreando órdenes de suspensión de protección (`SellStop` para largo, `BuyStop` para corto). La estrategia almacena el último nivel de parada para imitar los umbrales finales MetaTrader.
* El cierre forzado opcional (`pre_init`) sigue la lógica original: una vez que el mercado supera `MaxStopLossPoints`, la posición se cierra inmediatamente.
* No se ha agregado ninguna lógica de entrada; users should combine this trailing module with their own signal provider.

## Consejos de uso

1. Adjunte la estrategia al mismo valor que abre las posiciones.
2. Ajuste las distancias de los puntos al tamaño del tick del instrumento (los símbolos Forex generalmente usan valores de "pip", los CFD pueden requerir diferentes multiplicadores).
3. Establezca `TrailStepPoints` en un valor positivo para reducir la rotación de pedidos en instrumentos ilíquidos.
4. Disable `EnforceMaxStopLoss` if another risk manager already controls hard stop distances.
5. Mantenga `ShowIndicator` habilitado mientras ajusta los parámetros para visualizar la media móvil y el comportamiento final.
