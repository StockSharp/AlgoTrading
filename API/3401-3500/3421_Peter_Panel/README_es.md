# Estrategia del panel de Peter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Peter Panel** traslada el panel de control discrecional MetaTrader 5 "Peter Panel" a StockSharp. El asesor experto original dibujó tres líneas horizontales (entrada, toma de ganancias y límite de pérdidas) y una matriz de botones que permitía al operador enviar instantáneamente órdenes de mercado o pendientes usando esos niveles. Esta estrategia de C# mantiene intacto el flujo de decisiones al tiempo que reemplaza el panel gráfico con parámetros de estrategia interactivos. Cada botón se comporta como el botón original: configurar el parámetro en `true` realiza la acción inmediatamente y la bandera se restablece a `false`.

## Conceptos clave

1. **Asistente manual** – la estrategia no genera señales. Usted decide cuándo operar alternando los parámetros expuestos en la interfaz de usuario de la estrategia o los scripts de automatización.
2. **Líneas de precios compartidas**: la línea de entrada color aguamarina, la línea verde de obtención de beneficios y la línea roja de limitación de pérdidas están representadas por tres parámetros decimales. Sus valores se pueden establecer manualmente o recalcularse en torno al precio medio actual mediante el interruptor `ResetCommand`.
3. **Cobertura completa de órdenes**: se implementan los seis tipos de órdenes del panel: compra/venta de mercado, parada de compra, límite de compra, parada de venta y límite de venta. Las órdenes de protección se adjuntan después de cada llenado, emulando los campos TP/SL que el panel MetaTrader completó automáticamente.
4. **Modificaciones masivas**: el parámetro `ModifyCommand` vuelve a aplicar las líneas de precio actuales a cada orden pendiente activa y a las órdenes protectoras de stop-loss/take-profit de la posición abierta.
5. **Liquidación con un solo toque**: el botón `CloseCommand` cancela las órdenes pendientes pendientes, elimina las órdenes de protección y nivela la posición neta en el mercado.

## Implementación original frente a StockSharp

| Característica | MetaTrader Panel de 5 Pedro | StockSharp Estrategia del Panel Peter |
| --- | --- | --- |
| Interfaz de usuario | Diálogo en el gráfico con botones y campos editables | Parámetros de estrategia que se comportan como interruptores y entradas numéricas. |
| Manipulación de entrada/TP/SL | Arrastre líneas horizontales o presione "Restablecer" para volver a centrar | Edite los valores de los parámetros directamente o use el botón `ResetCommand` |
| Envío de pedidos | El botón activa la solicitud `OrderSend` sincrónica | La alternancia de parámetros llama al ayudante `Buy/Sell` correspondiente y almacena las referencias de pedidos |
| Manejo de TP/SL | Completado hasta `MqlTradeRequest.tp` y `.sl` en cada pedido | La parada de protección y el objetivo se registran como órdenes de parada/límite separadas inmediatamente después del cumplimiento. |
| Modificación de pedido | Seleccione un ticket de la lista y presione "Modificar" | `ModifyCommand` cancela/reemplaza cada orden pendiente activa y actualiza las órdenes de protección |
| Cierre de pedido | Presione "Cerrar" en el ticket resaltado | `CloseCommand` cierra toda la posición y cancela todas las órdenes pendientes y de protección |
| lista de pedidos | Tabla gráfica de tickets y niveles. | La estrategia se basa en el seguimiento de pedidos de StockSharp; El estado detallado está disponible en los registros. |

> **Nota:** MetaTrader permitió al comerciante seleccionar un solo boleto de la lista. El puerto StockSharp aplica modificaciones y cierres a cada orden creada por la estrategia porque una selección directa de boleto único no está disponible dentro de los parámetros de la estrategia.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `Volume` | Volumen comercial en lotes. Se valida según el paso de volumen de seguridad y los límites mínimo/máximo. |
| `EntryLevel` | Precio utilizado para órdenes pendientes (línea aqua). |
| `TakeProfitLevel` | Precio de la línea verde. Actúa como nivel de obtención de beneficios para operaciones largas y como nivel de parada protectora para operaciones cortas, reflejando el panel original. |
| `StopLossLevel` | Precio de línea roja. Actúa como parada protectora para operaciones largas y como objetivo de obtención de beneficios para operaciones cortas. |
| `BuyMarketCommand` | Envíe una orden de compra de mercado cuando esté configurada en `true`. La bandera se restablece a `false` después de enviar el pedido. |
| `BuyStopCommand` | Coloque una orden de parada de compra en `EntryLevel`. |
| `BuyLimitCommand` | Realice una orden de límite de compra en `EntryLevel`. |
| `SellMarketCommand` | Envíe una orden de venta de mercado. |
| `SellStopCommand` | Coloque una orden de parada de venta en `EntryLevel`. |
| `SellLimitCommand` | Realice una orden de límite de venta en `EntryLevel`. |
| `ModifyCommand` | Vuelva a aplicar `EntryLevel`, `TakeProfitLevel` y `StopLossLevel` a las órdenes pendientes existentes y a las órdenes de protección de la posición actual. |
| `CloseCommand` | Cancelar órdenes pendientes, eliminar órdenes de protección y nivelar la posición en el mercado. |
| `ResetCommand` | Vuelva a calcular los tres niveles de precios alrededor del punto medio actual de oferta y demanda. |

## Flujo de trabajo

1. Inicie la estrategia una vez que el valor y la cartera deseados estén conectados. La suscripción de nivel 1 actualiza el caché interno de oferta/demanda que impulsa la función `ResetCommand`.
2. Utilice el interruptor `ResetCommand` o las ediciones manuales para configurar los niveles de precios aguamarina, verde y rojo.
3. Active una operación cambiando uno de los parámetros de acción a `true`. La estrategia restablece automáticamente el interruptor a `false` para que la siguiente activación sea intencional.
4. Después de completarse, la estrategia envía las órdenes apropiadas de stop-loss y take-profit según la dirección de la posición. Por ejemplo, una posición larga obtiene un stop de venta en la línea roja y un límite de venta en la línea verde, mientras que una posición corta recibe la combinación inversa.
5. Modifique los niveles en cualquier momento y presione `ModifyCommand` para actualizar las órdenes pendientes y las salidas protectoras sin reiniciar la estrategia.
6. Cuando finalice la sesión de negociación, active `CloseCommand` para aplanar y limpiar todas las órdenes administradas por la estrategia.

## Diferencias con el panel original

- No hay una lista gráfica de entradas. En cambio, los registros StockSharp realizan un seguimiento de cada orden y comercio registrado. Puede conectar la estrategia a cualquier interfaz de usuario externa si se requiere gestión de tickets individuales.
- Los valores de stop-loss y take-profit se implementan como órdenes secundarias explícitas porque StockSharp no puede incrustar los precios TP/SL directamente en la solicitud de orden principal. El comportamiento coincide con el resultado final del panel MetaTrader: la posición acaba protegida por los mismos niveles.
- El reemplazo de pedidos se realiza mediante ciclos de cancelación y recreación. Esto mantiene el flujo de trabajo determinista incluso en lugares que no admiten modificaciones in situ.

## Consejos de uso

- Combine la estrategia con gráficos o paneles StockSharp para recrear la experiencia del panel original, reemplazando los botones del gráfico con elementos de la interfaz de usuario que alternan los parámetros expuestos.
- La estrategia no pone en cola múltiples acciones. Si necesita automatizar secuencias (por ejemplo, restablecer niveles y luego realizar un pedido pendiente), active los cambios secuencialmente después de que el anterior se restablezca a `false`.
- Las órdenes de protección solo se crean para posiciones distintas de cero. Si realiza órdenes pendientes sin una posición, llame a `ModifyCommand` después de que se complete la orden para asegurarse de que se apliquen los últimos niveles.

## Consideraciones de seguridad

- Verifique siempre que la información de la cartera, el valor y el paso del precio estén disponibles antes de enviar cualquier pedido. La estrategia registra advertencias cuando faltan datos requeridos.
- El parámetro `Volume` está sujeto a los límites del instrumento. Si el volumen ajustado llega a cero debido a un paso o volumen mínimo incompatible, no se envía ninguna orden y aparece una advertencia en el registro.
- Cuando se ejecuta `CloseCommand`, la estrategia primero cancela las órdenes de protección, luego las órdenes pendientes y finalmente aplana la posición. Esto refleja el orden defensivo de las operaciones del asesor experto original.
