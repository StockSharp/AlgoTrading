# Estrategia de tendencia de seguimiento de línea
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Follow Line es un puerto directo del MetaTrader asesor experto `FollowLineEA_v1.0`. Replica la lógica original al combinar un detector de ruptura de banda Bollinger con una línea de tendencia adaptativa que abraza la acción del precio. La estrategia escucha las velas terminadas y funciona en cualquier período de tiempo proporcionado por el usuario.

Una ruptura por encima de la banda superior Bollinger eleva la línea de soporte por debajo del precio, mientras que un cierre por debajo de la banda inferior hace caer una línea de resistencia por encima del precio. La línea se desliza sólo en la dirección de ruptura, creando un patrón de escalera que resalta las tendencias sostenidas. El relleno opcional ATR puede ampliar la línea para evitar que las posiciones se activen demasiado pronto. Los filtros de impulso basados ​​en promedios móviles confirman las entradas según el modo de flecha seleccionado.

## Lógica de trading
1. **Cadena de indicadores**
   - Bollinger Bandas (largo = `BollingerPeriod`, ancho = `BollingerDeviations`).
   - ATR opcional (longitud = `AtrPeriod`) para compensar la línea de tendencia cuando `UseAtrFilter` está habilitado.
   - Una familia de promedios móviles simples (longitud = `MovingAveragePeriod`) aplicada a precios máximos, mínimos, de apertura, de cierre y medianos. Estos promedios generan indicadores de confirmación cuando `TypeOfArrows` se establece en `OpenCloseMedian` o `HighLowOpenClose`.
2. **Actualización de la línea de tendencia**
   - Una vela que se cierra por encima de la banda superior empuja la línea de tendencia hacia el mínimo de la vela (menos ATR compensación si se usa) pero nunca la baja.
   - Una vela que se cierra por debajo de la banda inferior arrastra la línea hasta el máximo de la vela (más ATR compensación si se usa) pero nunca la levanta.
   - La dirección de la línea de tendencia define si el mercado se considera alcista (>0) o bajista (<0).
3. **Señales de entrada**
   - Cuando la dirección cambia de bajista a alcista y los filtros de flecha coinciden, se pone en cola una flecha de compra.
   - Cuando la dirección cambia de alcista a bajista, se pone en cola una flecha de venta.
   - El parámetro `IndicatorsShift` retrasa la ejecución para que la flecha pueda procesarse `IndicatorsShift` barras después de formarse, imitando el cambio del búfer MT4.
4. **Filtros de ejecución**
   - Filtro de tiempo: las operaciones solo se permiten entre `TimeStartTrade` y `TimeEndTrade` cuando `UseTimeFilter` está habilitado (la ventana puede finalizar hasta medianoche).
   - Filtro de diferencial: si el diferencial actual supera `MaxSpread` (medido en incrementos de precio), las órdenes se omiten.
   - Límite de orden: `MaxOrders` limita el tamaño absoluto de la posición para replicar la verificación original de "órdenes máximas".

## Gestión del riesgo
- **Salir en señal opuesta**: establezca `CloseInSignal` en `true` para aplanar inmediatamente la exposición existente cuando se dispare la flecha opuesta.
- **Bloqueos de cesta**: `CloseInProfit` y `CloseInLoss` cierran la posición actual una vez que se alcanza el objetivo de pip especificado. `UseBasketClose` aplica los umbrales a toda la cesta en lugar de separar la lógica larga y corta (refleja la implementación de MQL).
- **Paradas y objetivos**: la estrategia llama a `SetStopLoss`, `SetTakeProfit`, el seguimiento y el punto de equilibrio protegen cada barra cuando los conmutadores correspondientes están habilitados (`UseStopLoss`, `UseTakeProfit`, `UseTrailingStop`, `UseBreakEven`). Todas las distancias se expresan en pasos de precio.
- **Tamaño del lote**: cuando `AutoLotSize` está habilitado, el tamaño de la posición es igual a la parte seleccionada del valor actual de la cartera (`RiskFactor` por ciento). De lo contrario, se utiliza un `ManualLotSize` fijo. El monto está normalizado al paso de volumen del instrumento y está limitado por límites de cambio.

## Parámetros
| grupo | Nombre | Descripción |
| --- | --- | --- |
| generales | `CandleType` | Plazo o tipo de vela personalizado utilizado para la suscripción. |
| Indicador | `BarsCount` | Profundidad histórica utilizada por el indicador. |
| Indicador | `BollingerPeriod` / `BollingerDeviations` | Configuración Bollinger para detección de fugas. |
| Indicador | `MovingAveragePeriod` | Longitud de las medias móviles que impulsan los filtros de flecha. |
| Indicador | `AtrPeriod` / `UseAtrFilter` | ATR longitud y bandera de activación. |
| Indicador | `TypeOfArrows` | Modo de flecha (`HideArrows`, `SimpleArrows`, `OpenCloseMedian`, `HighLowOpenClose`). |
| Indicador | `IndicatorsShift` | Retraso (en barras) entre la formación de la flecha y su ejecución. |
| tiempo | `UseTimeFilter`, `TimeStartTrade`, `TimeEndTrade` | Límites de sesión. |
| Filtros | `MaxSpread`, `MaxOrders` | Techo de extensión y límite de posición. |
| Riesgo | `CloseInSignal`, `UseBasketClose`, `CloseInProfit`, `PipsCloseProfit`, `CloseInLoss`, `PipsCloseLoss` | Normas de gestión de la cesta. |
| Riesgo | `UseTakeProfit`, `TakeProfit`, `UseStopLoss`, `StopLoss`, `UseTrailingStop`, `TrailingStop`, `TrailingStep`, `UseBreakEven`, `BreakEven`, `BreakEvenAfter` | Conjunto de órdenes de protección (valores en incrementos de precios). |
| Gestión del dinero | `AutoLotSize`, `RiskFactor`, `ManualLotSize` | Dimensionamiento de la posición. |

## Notas de uso
- La estrategia opera únicamente con velas terminadas. Por lo tanto, es seguro realizar una prueba retrospectiva con la misma compresión de barras que el comercio real.
- La cola personalizada detrás de `IndicatorsShift` mantiene el comportamiento de alto nivel API idéntico al acceso al búfer del indicador MT4 (`iCustom(..., shift)`).
- `TypeOfArrows = HideArrows` desactiva el comercio mientras conserva la lógica de dibujo del indicador, exactamente igual que la fuente EA.
- Para visualizar las operaciones, adjunte la estrategia a un área del gráfico después de llamar a `CreateChartArea()` (ya manejado en `OnStarted`).

## Detalles de conversión
- La lógica se basa exclusivamente en indicadores StockSharp integrados y en la suscripción de vela de alto nivel API (sin almacenamiento en búfer manual ni llamadas `GetValue`).
- La gestión de pedidos se realiza con `BuyMarket`/`SellMarket` más los métodos auxiliares `SetStopLoss` y `SetTakeProfit`, reflejando el comportamiento MT4 del código original.
- El tamaño de lote basado en la cartera respeta los límites de intercambio a través de cheques `VolumeStep`, `VolumeMin` y `VolumeMax` antes de enviar pedidos.
- La estrategia conserva comentarios de código en inglés y descripciones de parámetros para alinearse con las pautas del repositorio.
