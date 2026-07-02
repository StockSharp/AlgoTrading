# Estrategia TrailingStopFrCn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

`TrailingStopFrCnStrategy` es un puerto StockSharp del asesor experto MetaTrader **TrailingStopFrCn.mq4**. El script original gestiona los niveles de stop-loss para posiciones existentes utilizando una combinación de distancias finales fijas, fractales de Bill Williams o máximos/mínimos de velas recientes. Este puerto mantiene la misma flexibilidad mientras se integra con el StockSharp API de alto nivel: la estrategia se suscribe a velas y cotizaciones de Nivel 1, monitorea la posición neta actual y actualiza automáticamente una orden de parada de protección.

A diferencia de una estrategia de entrada, TrailingStopFrCn se centra únicamente en la gestión de riesgos. No abre nuevos puestos. En su lugar, rastrea la posición existente de `Strategy.Security`, cancela órdenes stop obsoletas cuando la posición cambia y envía una única orden stop agregada que sigue la lógica del asesor MetaTrader.

## Lógica final

1. **Distancia de seguimiento fija**: cuando `TrailingStopPips` es mayor que cero, la estrategia se comporta como el parámetro MQL original `TrailingStop`. Para posiciones largas el stop se coloca en `bestBid - distance`, para posiciones cortas en `bestAsk + distance`, con `distance = TrailingStopPips × pip size`.
2. **Seguimiento de fractales**: cuando `TrailingStopPips = 0` y `TrailingMode = Fractals`, la estrategia detecta fractales Bill Williams de cinco barras. Cada vela terminada se agrega a un búfer interno y, una vez que hay suficiente historial disponible, la vela dos barras atrás se evalúa como un fractal potencial. El fractal más reciente que esté al menos a `MinStopDistancePips` del precio actual se convierte en el nuevo candidato a parada.
3. **Seguimiento de velas**: cuando `TrailingStopPips = 0` y `TrailingMode = Candles`, la estrategia escanea hasta las últimas 99 velas cerradas y selecciona el primer mínimo (para largos) o máximo (para cortos) que esté separado del precio actual por al menos `MinStopDistancePips`.

Después de calcular el nivel del candidato, la estrategia aplica las mismas reglas de protección que la versión MQL:

- **OnlyProfit** impide mover el stop a menos que el nuevo nivel bloquee las ganancias (stop por encima de la entrada para posiciones largas, stop por debajo de la entrada para posiciones cortas).
- **OnlyWithoutLoss** detiene el seguimiento posterior una vez que el stop-loss activo ya protege la posición contra pérdidas (en el script original, el proceso de seguimiento se detiene después de alcanzar el punto de equilibrio).
- El stop sólo se mueve en la dirección favorable: hacia arriba para posiciones largas y hacia abajo para posiciones cortas.

Debido a que StockSharp rastrea una única posición neta por valor, el volumen de la orden de suspensión es igual a `Math.Abs(Position)` y se agregan todas las ejecuciones subyacentes.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `OnlyProfit` | Mueva el stop-loss sólo cuando el nuevo nivel garantice ganancias en relación con el precio de entrada promedio. Refleja la bandera `OnlyProfit` de MQL. |
| `OnlyWithoutLoss` | Deje de seguir el rastro una vez que el stop-loss activo esté en el precio de entrada o lo supere. Esto replica `OnlyWithoutLoss` del asesor original. |
| `TrailingStopPips` | Distancia de seguimiento fija expresada en pips. Establezca en cero para activar el fractal o el seguimiento de velas. |
| `MinStopDistancePips` | Distancia mínima (en pips) entre el precio de mercado y el stop-loss. Úselo para emular la restricción del corredor `MODE_STOPLEVEL`. |
| `TrailingMode` | Elige la fuente final cuando `TrailingStopPips = 0`. Opciones: `Fractals` (Bill Williams fractales de cinco barras) o `Candles` (mínimos/máximos recientes). |
| `CandleType` | Tipo de datos de vela utilizado para construir fractales o buscar puntos de oscilación. El valor predeterminado es un período de tiempo de una hora. |

## Notas de comportamiento

- La estrategia se suscribe a datos de Nivel 1 para acceder a los mejores precios de oferta y demanda. El seguimiento de distancia fija reacciona inmediatamente a las actualizaciones de Nivel 1, mientras que el seguimiento de fractales/velas se actualiza cuando llegan nuevas velas.
- Cuando la dirección de la posición cambia, la orden de parada actual se cancela antes de que se envíe la nueva orden.
- Si no hay ningún candidato de parada disponible (por ejemplo, no hay suficientes velas), la estrategia mantiene la parada existente.
- Si el corredor no impone una distancia mínima de parada, puede dejar `MinStopDistancePips` en cero.

## Diferencias con la versión MetaTrader

- StockSharp mantiene una posición neta, por lo que no se realiza un seguimiento de los "tickets" individuales de MetaTrader. La orden de parada cubre toda la posición agregada.
- El filtro `Magic` no es necesario: la estrategia ya opera en su propio contexto de seguridad.
- Las actualizaciones finales son impulsadas por velas terminadas más datos de Nivel 1 en lugar de un ciclo de sondeo de un segundo.
- Los objetos de gráficos visuales del EA original no se recrean; en su lugar, puede utilizar los asistentes de gráficos de StockSharp cuando ejecute la interfaz de usuario de muestra.

## Consejos de uso

1. Ejecute la estrategia junto con cualquier lógica de entrada que abra posiciones en el mismo `Security`. TrailingStopFrCn adjuntará automáticamente una orden de parada una vez que aparezca la posición.
2. Ajuste `CandleType` para que coincida con el período de tiempo que debe analizarse en busca de fractales o puntos de oscilación. Los plazos más altos suavizan los niveles de seguimiento, mientras que los plazos más bajos reaccionan más rápido.
3. Calibre `MinStopDistancePips` según las limitaciones de nivel de parada de su corredor. Configurarlo demasiado bajo puede provocar el rechazo de pedidos.
4. Al realizar pruebas con datos históricos, asegúrese de que la suscripción de velas y los mensajes de Nivel 1 estén disponibles en la fuente de datos para que la lógica de seguimiento pueda activarse correctamente.
