# Estrategia RobotPowerM5 Meta4 V12
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia RobotPowerM5 Meta4 V12 es una versión C# del asesor experto MetaTrader 4 `RobotPowerM5_meta4V12.mq4`. El EA original
fue diseñado para gráficos Forex de cinco minutos y evalúa el equilibrio entre Bulls Power y Bears Power para decidir si un nuevo
Se debe abrir una posición larga o corta. La versión StockSharp mantiene el comportamiento de una posición a la vez, reproduce el punto
ajustes basados en stop-loss/take-profit, y reimplementa la lógica de trailing-stop que bloquea gradualmente las ganancias una vez que el mercado
se mueve a favor del comercio.

## Lógica de trading
1. **Motor indicador**
   - Las velas de cinco minutos están suscritas de forma predeterminada (el período de tiempo se puede configurar a través del parámetro `CandleType`).
   - Un par de indicadores StockSharp, `BullsPower` y `BearsPower`, se actualizan en cada vela terminada usando el configurado
período de promediación.
   - El valor combinado `BullsPower + BearsPower` se almacena con un retraso de una barra para imitar las llamadas `shift=1` del
Código MQL, que siempre opera en la última barra completamente cerrada.
2. **Reglas de entrada**
   - Cuando no hay ninguna posición abierta y la suma retrasada de Bulls/Bears Power es **positiva**, se emite una orden de compra de mercado.
   - Cuando no hay ninguna posición abierta y la suma retrasada es **negativa**, se emite una orden de venta de mercado.
   - Las señales se ignoran mientras una posición está activa; el comercio se gestiona exclusivamente a través de salidas protectoras.
3. **Manejo de volumen**
   - El parámetro `Volume` representa el tamaño de lote solicitado. Se pasa directamente a `BuyMarket` / `SellMarket`, lo que permite
conector para redondear al paso del lote del instrumento si es necesario.

## Gestión del riesgo
- **Stop-loss**: el stop inicial se coloca a `StopLossPoints` MetaTrader puntos del precio de ejecución promedio. El nivel es
monitoreado con mínimos de velas (para largos) o máximos (para cortos); Una vez tocada la estrategia de salidas al mercado.
- **Take-profit**: el objetivo de ganancias es `TakeProfitPoints` puntos desde la entrada y se evalúa según los máximos y mínimos de las velas, coincidiendo
cómo MT4 cierra posiciones cuando se golpea un objetivo dentro de la barra.
- **Trailing stop**: después de que el precio se mueve a favor de la operación en más de `TrailingStopPoints`, se activa un trailing stop.
Para posiciones largas, el stop se desplaza a `referencePrice - trailingDistance`, donde la referencia es el máximo de la vela.
cerca y alto. Para cortos, el stop sigue `referencePrice + trailingDistance`, utilizando el mínimo del cierre y el mínimo de la vela.
Esto reproduce el comportamiento de seguimiento de EA que se implementó originalmente con `OrderModify`.

## Parámetros
| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `BullBearPeriod` | Período promedio proporcionado a los indicadores Bulls Power y Bears Power. | `5` | Aumentar el valor suaviza el filtro de impulso. |
| `Volume` | Tamaño de lote solicitado para cada entrada. | `1` | El volumen real negociado depende del paso del lote y de los límites del corredor. |
| `StopLossPoints` | Distancia de parada de protección inicial en MetaTrader puntos. | `45` | Establezca en `0` para desactivar el stop-loss duro. |
| `TakeProfitPoints` | Distancia de obtención de beneficios en MetaTrader puntos. | `150` | Establezca en `0` para operar sin un objetivo de ganancias fijo. |
| `TrailingStopPoints` | Distancia utilizada por el trailing stop una vez que la operación es rentable. | `15` | Establezca en `0` para deshabilitar el seguimiento. |
| `CandleType` | Plazo utilizado para los cálculos de los indicadores. | `5m time frame` | Se puede seleccionar cualquier otro `DataType` si es necesario. |

## Notas de implementación
- La estrategia almacena todos los niveles de riesgo (stop-loss, take-profit, trailing stop) internamente y emite salidas de mercado cuando las velas
confirmar que se superó un umbral de precio. Esto refleja el enfoque MT4 donde las órdenes se modificaban paso a paso.
- Las suscripciones a indicadores se cablean a través de `Subscription.Bind`, que alimenta tanto Bulls Power como Bears Power en una única devolución de llamada.
- El tamaño en puntos se deriva de `Security.PriceStep`, manteniendo los parámetros compatibles con instrumentos que cotizan en ticks,
pips o centavos.
- Las comprobaciones de entrada siempre utilizan los valores del indicador *anterior*, lo que garantiza que las velas parcialmente formadas nunca activen órdenes.

## Diferencias frente a la versión MQL
- La gestión comercial utiliza salidas explícitas del mercado en lugar de modificar la orden de límite de pérdidas vigente; esto es más robusto en
diferentes conectores StockSharp y producir el mismo resultado.
- Los rangos de parámetros se validan a través de `StrategyParam` ayudantes para que los valores no válidos (por ejemplo, paradas finales negativas) sean
rechazado en el momento de la configuración.
- Los enlaces de registro detallados, la salida de gráficos y las suscripciones de velas aprovechan el API de alto nivel de StockSharp en lugar de los bucles de ticks manuales.
- La cadena de identificador de experto presente en el script MT4 no es necesaria en StockSharp y, por lo tanto, se ha omitido.
