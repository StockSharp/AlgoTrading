# Estrategia de pivote L3H3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de pivote L3H3** es una adaptación StockSharp del experto MetaTrader "L3_H3_Expert". El guión original construye una estructura de pivote diaria y despliega dos órdenes pendientes para negociar posibles rupturas o retrocesos alrededor de los máximos y mínimos de la sesión anterior. La versión StockSharp mantiene la misma idea: recalcula los niveles de pivote después de cada vela completa de período de tiempo más alto (diariamente de forma predeterminada) y decide entre detener o limitar las entradas en función de dónde cotiza actualmente el mercado en relación con el rango de ayer.

## Lógica de trading

1. **Estadísticas de la sesión**
   - Después de cada vela pivote completada (predeterminado: diariamente), la estrategia captura los valores de apertura, máximo, mínimo y cierre de la sesión anterior.
   - El nivel de pivote clásico se calcula como `(High + Low + Close) / 3`.
   - Estos niveles permanecen activos durante toda la siguiente sesión.

2. **Configuración de entrada**
   - Un precio de entrada de compra está anclado ligeramente por encima del mínimo anterior. El desplazamiento es igual al parámetro `EntryOffsetPips` expresado en múltiplos de tamaño de pip.
   - Un precio de entrada de venta está anclado en el máximo anterior (reflejando al experto original que utilizó el máximo bruto sin ningún amortiguador adicional).
   - Para cada nuevo día de negociación (detectado a través de la suscripción de vela principal), la estrategia coloca nuevas órdenes pendientes:
     - Si el mercado cotiza **por debajo** del mínimo de ayer, se coloca un **parada de compra** para capturar una ruptura alcista.
     - Si el mercado cotiza **por encima** del máximo de ayer, se coloca un **stop-de venta** para negociar una reversión a la baja.
     - De lo contrario, el algoritmo prefiere órdenes **limitadas** en los mismos niveles de precios para comprar caídas o vender repuntes nuevamente dentro del rango.
   - Las órdenes de stop-loss se colocan `StopLossPips` lejos del mínimo/máximo de referencia, exactamente como la versión MQL fijó un stop buffer de 16 puntos.
   - La toma de ganancias de ambas órdenes pendientes está alineada con el nivel de pivote, replicando la ubicación objetivo que se encuentra en el código fuente.

3. **Gestión de pedidos**
   - Cada vez que se calcula un nuevo pivote, cualquier orden pendiente en funcionamiento se cancela y se recalcula con los nuevos niveles.
   - La estrategia también cancela órdenes pendientes obsoletas cuando comienza una nueva sesión, evitando la acumulación de órdenes inactivas.
   - Cuando se completa una orden, su referencia interna se borra automáticamente para evitar cancelaciones duplicadas.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `EntryCandleType` | Serie de velas utilizadas para monitorear la sesión actual y activar la colocación de órdenes. | marco de tiempo de 5 minutos |
| `PivotCandleType` | Vela de marco temporal más alto utilizada para medir las estadísticas de la sesión anterior. | Marco de tiempo diario |
| `EntryOffsetPips` | Distancia (en pips) agregada por encima del mínimo anterior para entradas largas. | 2 |
| `StopLossPips` | Distancia (en pips) aplicada más allá del mínimo/máximo de referencia para posicionar el stop loss. | 16 |

## Diferencias con el experto MQL

- El script MetaTrader seleccionó diferentes sesiones de negociación (Asiática, Londres, Nueva York) mediante números mágicos y ventanas de tiempo. La versión StockSharp consolida el comportamiento mediante el uso de una vela de período de tiempo más alto configurable (diaria de forma predeterminada) para derivar los niveles de pivote, lo que hace que la lógica sea más fácil de auditar y adaptar entre corredores.
- MetaTrader se basó en la oferta/demanda actual para decidir entre órdenes stop y límite. La implementación StockSharp utiliza la vela terminada más reciente de la serie `EntryCandleType` para esa comparación a fin de mantener el flujo de trabajo basado en eventos.
- Los comentarios de pedidos y los números mágicos eran específicos de la plataforma en MT4. Se omiten aquí intencionadamente; en cambio, la estrategia mantiene referencias directas a sus órdenes pendientes.

## Notas de uso

- Asegúrese de que la seguridad subyacente exponga un `PriceStep` válido. La estrategia genera una excepción al inicio si la conexión del corredor no proporciona información sobre el tamaño del pip.
- Para replicar el comportamiento original más fielmente, configure `PivotCandleType` en una serie de velas horarias agregadas durante la sesión deseada y ajuste los parámetros de compensación/detención en consecuencia.
- Al igual que con cualquier estrategia de órdenes pendientes, considere la distancia mínima del corredor y las políticas de vencimiento de las órdenes pendientes al implementar en vivo.
