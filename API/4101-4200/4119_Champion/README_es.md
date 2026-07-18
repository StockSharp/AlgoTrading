# 4119 Estrategia de campeón
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un puerto C# de alto nivel del Asesor Experto MetaTrader ubicado en `MQL/919/champion.mq5`. El EA original espera una señal del índice de fuerza relativa (RSI) y coloca tres órdenes stop en la dirección de la ruptura anticipada. Cada orden pendiente ya incluye un límite de pérdidas y una toma de ganancias, y el límite de pérdidas se sigue cada vez que el precio se mueve favorablemente. La versión StockSharp mantiene el mismo comportamiento y depende exclusivamente de llamadas API de alto nivel (`SubscribeCandles`, `Bind`, `BuyStop`, `SellStop`, etc.).

La configuración predeterminada apunta a instrumentos FX líquidos donde el "punto" MetaTrader coincide con el StockSharp `PriceStep` (normalmente 0,0001). El tipo de vela es configurable y la estrategia se puede aplicar a cualquier período de tiempo siempre que la cuenta proporcione las mejores cotizaciones de oferta/demanda y, opcionalmente, información del nivel de parada.

## Lógica estratégica
1. **Generación de señal**
   - Se calcula un RSI de longitud configurable sobre velas completadas.
   - El valor RSI anterior (hace una barra cerrada) se compara con un umbral simétrico (`RsiLevel`).
   - `RSI < RsiLevel` desencadena una configuración alcista; `RSI > 100 - RsiLevel` desencadena una configuración bajista.
2. **Pendiente de realizar el pedido**
   - Cuando no hay posiciones abiertas ni órdenes pendientes activas gestionadas por la estrategia, se colocan tres órdenes stop idénticas en la dirección indicada.
   - Las paradas de compra se colocan por encima de la mejor oferta y las paradas de venta por debajo de la mejor oferta. La distancia respeta el nivel de parada proporcionado por el servidor (si está disponible) o el respaldo `MinOrderDistancePoints`.
   - El volumen del pedido se calcula dinámicamente: el valor de la cuenta disponible se divide por `BalancePerLot`, se fija en el rango de lote `[0.1, 15]` y se redondea a dos decimales. Cada orden pendiente recibe un tercio del volumen calculado.
3. **Órdenes de protección iniciales**
   - Tan pronto como se completa la primera operación, se registran órdenes de protección agregadas: stop-loss en `entry ± StopLossPoints` y take-profit en `entry ± TakeProfitPoints` (MetaTrader puntos convertidos en precio por `PriceStep`).
   - Si `TakeProfitPoints` es cero, la orden de obtención de beneficios se desactiva.
4. **Parada de seguimiento**
   - Mientras una posición está abierta, la orden de límite de pérdidas se ajusta con cada actualización de nivel 1.
   - Para largos, el nuevo stop es igual a `max(entry + spread, bid - StopLoss)`; para cortos `min(entry - spread, ask + StopLoss)`.
   - El seguimiento se activa solo cuando el movimiento excede la suma del nivel de parada del corredor y el diferencial actual, reproduciendo las salvaguardias originales EA.
5. **Pendiente de mantenimiento de pedidos**
   - Las paradas de compra pendientes se acercan al mercado cuando su precio de activación está a más de `RepriceDistancePoints` de la demanda actual. La misma lógica se aplica a las paradas de venta frente a la oferta actual.
   - La revisión de precios siempre respeta el mayor de `RepriceDistancePoints` y la distancia efectiva del nivel de parada.
6. **Posición de salida**
   - Las posiciones se cierran mediante órdenes protectoras de stop-loss/take-profit o mediante la intervención manual del usuario. Cuando el tamaño de la posición vuelve a cero, la estrategia cancela las órdenes de protección restantes y espera la siguiente señal RSI.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitPoints` | MetaTrader puntos agregados/restados del precio de ejecución para realizar la orden de obtención de ganancias. Establezca en `0` para desactivar el objetivo. |
| `StopLossPoints` | MetaTrader puntos agregados/restados del precio de ejecución para realizar la orden de límite de pérdidas y calcular la distancia de seguimiento. |
| `RsiPeriod` | RSI longitud (número de velas). |
| `RsiLevel` | Umbral simétrico RSI. Los valores por debajo del nivel activan posiciones largas, los valores por encima de `100 - level` activan posiciones cortas. |
| `BalancePerLot` | El monto de la moneda de la cuenta se considera equivalente a un lote estándar al dimensionar las posiciones. |
| `MinOrderDistancePoints` | Distancia mínima de retroceso (en puntos) entre el precio de mercado y las nuevas órdenes stop cuando el centro de negociación no informa un nivel stop. |
| `RepriceDistancePoints` | Distancia (en puntos) que activa la revisión de precios de órdenes pendientes. |
| `CandleType` | Tipo de datos de vela utilizado para el cálculo RSI. |

## Notas de uso
- La estrategia requiere tanto datos de velas como cotizaciones de nivel 1 (mejor oferta/demanda). Sin actualizaciones de nivel 1, la lógica de seguimiento y el mantenimiento de órdenes pendientes están deshabilitados.
- Cuando el corredor expone un nivel de parada o una distancia de parada a través de metadatos de nivel 1, se respeta automáticamente. De lo contrario, configure `MinOrderDistancePoints` para que coincida con los requisitos del instrumento.
- El tamaño de la posición vuelve a la propiedad `Strategy.Volume` siempre que falta información de la cartera o el tamaño del lote calculado deja de ser positivo.
- Siempre se colocan tres órdenes pendientes juntas. Cancelar pedidos no deseados manualmente si se requiere participación parcial; la estrategia seguirá gestionando los restantes.

## Gestión de riesgos
- Las órdenes de stop-loss y take-profit son órdenes nativas de bolsa/corredor, que reflejan el comportamiento de MetaTrader EA. Cuando se cierra una posición, las órdenes de protección se cancelan inmediatamente.
- El trailing stop sólo se mueve en dirección al beneficio y nunca afloja el stop-loss. Se activa una vez que el precio ha recorrido al menos `(StopLevel + spread)` más allá del precio de entrada.
- La lógica de revisión de precios evita que las órdenes pendientes obsoletas se queden atrás después de grandes saltos, lo que reduce la probabilidad de cumplimientos retrasados.
