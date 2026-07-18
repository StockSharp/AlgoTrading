# Roman Cambio de Dirección
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea el asesor experto MQL original publicado como `roman.mq5`. Siempre mantiene una posición abierta y alterna la dirección de la operación solo después de cerrar la operación anterior. Mientras la posición sigue siendo rentable repite la misma dirección; tras un stop-loss la estrategia cambia al lado opuesto. La versión StockSharp trabaja con datos de nivel 1 y usa las mejores cotizaciones de oferta/demanda para emular las salidas basadas en pips de MetaTrader.

## Lógica de la estrategia
1. **Dirección inicial** – al inicio, el parámetro `StartWithBuy` define si la primera orden es una compra o una venta. La decisión se almacena en `_nextTradeBuy` para que persista entre operaciones.
2. **Entrada al mercado** – cuando la estrategia está plana y no hay órdenes pendientes, envía una orden de mercado en la dirección predefinida. Para órdenes de compra el mejor ask actual se almacena como precio de entrada de referencia, y para órdenes de venta se usa el mejor bid actual. Esto refleja la implementación de MetaTrader donde las compras se ejecutan al ask y las ventas al bid.
3. **Monitoreo de la posición abierta** – después de que la orden se rellena, la estrategia escucha actualizaciones de nivel 1. Cada actualización proporciona el último bid/ask para que el algoritmo pueda calcular la ganancia no realizada expresada en pasos de precio (pips). El `PriceStep` del instrumento se usa como denominador, con un valor de retroceso de `1` si el paso es desconocido.
4. **Regla de take-profit** – cuando la ganancia no realizada alcanza o supera `TakeProfitSteps`, la posición se cierra con `ClosePosition()`. El flag `_nextTradeBuy` mantiene el mismo valor para que la siguiente orden siga la dirección que acaba de tener éxito.
5. **Regla de stop-loss** – cuando la pérdida no realizada alcanza o supera `StopLossSteps`, la posición se cierra y `_nextTradeBuy` se alterna. La siguiente operación entra en la dirección opuesta, coincidiendo con el comportamiento del EA original donde el booleano `bs` cambia en una pérdida.
6. **Throttling de órdenes** – `_orderPending` evita que el algoritmo envíe múltiples órdenes mientras una solicitud anterior aún se está procesando. El flag se restablece en `OnPositionChanged` después de actualizar el tamaño de la posición.

Esta secuencia simple mantiene la estrategia invertida en todo momento y alterna la dirección solo después de una operación perdedora. Como resultado, el sistema se parece a un interruptor de seguimiento de tendencia: después de un stop-loss asume que la tendencia ha cambiado y sigue el nuevo lado.

## Parámetros
- `OrderVolume` *(decimal, predeterminado = 0.1)* – cantidad enviada con cada orden de mercado. Establecer al tamaño de contrato necesario para trading en vivo o simulaciones.
- `TakeProfitSteps` *(int, predeterminado = 46)* – número positivo de pasos de precio requeridos para activar el take-profit. Los pasos corresponden a `Security.PriceStep`, por lo que en un símbolo con tamaño de tick de 0.01 el valor predeterminado equivale a 0.46 unidades de precio.
- `StopLossSteps` *(int, predeterminado = 31)* – máximo movimiento adverso de precio (en pasos) antes de que la posición se cierre y la dirección se voltee.
- `StartWithBuy` *(bool, predeterminado = true)* – determina si la primera operación es larga (`true`) o corta (`false`). Las operaciones posteriores dependen de los resultados de posiciones anteriores.

Cada parámetro se expone a través de `StrategyParam<T>`, admite optimización (excepto el interruptor booleano), y es visible en la UI gracias a los metadatos `SetDisplay`.

## Detalles de datos y ejecución
- Se suscribe a `SubscribeLevel1()` para recibir las mejores cotizaciones de oferta/demanda. No se requieren datos de velas ni de indicadores.
- Usa `BuyMarket`/`SellMarket` para entradas y `ClosePosition()` para salidas, asegurando que la lógica permanezca cercana a la versión MQL que dependía de órdenes de mercado inmediatas.
- Almacena localmente el último bid/ask conocido para imitar el cálculo de ganancia basado en `_Point` de MetaTrader.

## Gestión de riesgo
- El take-profit y stop-loss fijos en pasos de precio garantizan que cada operación tenga niveles de salida predefinidos.
- El cambio de dirección después de una pérdida puede llevar a alternancia rápida en mercados choppy, por lo que el tamaño de posición (`OrderVolume`) debe calibrarse según la tolerancia al riesgo de la cuenta.
- Porque la estrategia casi siempre mantiene una posición, es sensible a gaps nocturnos y saltos repentinos de cotización; considerar salvaguardas externas si eso es una preocupación.

## Valores predeterminados
- `OrderVolume` = 0.1
- `TakeProfitSteps` = 46
- `StopLossSteps` = 31
- `StartWithBuy` = true

## Filtros
- **Categoría**: Seguimiento de tendencia / interruptor de dirección
- **Dirección**: Ambos (largo y corto)
- **Indicadores**: Ninguno
- **Stops**: Sí (take-profit y stop-loss de paso fijo)
- **Complejidad**: Básico
- **Marco temporal**: Tick / cotizaciones Level1
- **Estacionalidad**: No
- **Redes neuronales**: No
- **Divergencia**: No
- **Nivel de riesgo**: Alto (siempre en el mercado)

## Notas
- El EA original almacenaba la próxima dirección en un booleano llamado `bs`. El port para StockSharp mantiene la misma idea a través de `_nextTradeBuy` mientras agrega throttling de órdenes para evitar envíos duplicados.
- La granularidad del paso de precio importa: si su instrumento usa pips fraccionarios, ajuste los valores predeterminados para que los objetivos de ganancia/pérdida reflejen las cantidades monetarias deseadas.
