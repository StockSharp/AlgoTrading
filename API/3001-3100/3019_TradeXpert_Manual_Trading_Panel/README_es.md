# Estrategia del Panel de Trading Manual TradeXpert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
El asesor experto MQL5 original TradeXpert es un panel de trading operado manualmente que expone una colección de botones para abrir posiciones, colocar órdenes pendientes, aplicar stops de protección y revertir o cerrar rápidamente una operación existente. Este port en C# reproduce el mismo conjunto de herramientas dentro de StockSharp convirtiendo cada acción del panel en un parámetro de estrategia. La estrategia en sí misma no genera señales de trading; en su lugar escucha sus instrucciones manuales, ejecuta las órdenes solicitadas y supervisa salidas de protección en el flujo de velas entrantes.

## Funcionalidad Recreada
- **Acciones de mercado.** Solicitudes de uso único para órdenes de mercado `Buy` o `Sell` usando el volumen de operación configurado.
- **Órdenes pendientes.** Colocación de una sola vez de órdenes Buy Limit/Stop y Sell Limit/Stop usando un precio absoluto o un desplazamiento desde el último cierre de vela.
- **Gestión de protección.** Los niveles de stop-loss y take-profit pueden definirse como niveles de precio absolutos o como desplazamientos desde el precio de entrada registrado. La estrategia monitorea los extremos de las velas y cierra la posición con una orden de mercado cuando se viola un nivel de protección.
- **Controles de salida manual.** Los parámetros dedicados replican los botones Close y Reverse del panel MQL, permitiendo cerrar o invertir una posición a demanda.

## Lógica de la Estrategia
1. La estrategia se suscribe al tipo de vela especificado por `CandleType`. El flujo se usa para determinar el precio de cierre más reciente para desplazamientos y para detectar si se cruzaron niveles de protección.
2. En cada vela terminada la estrategia:
   - Aplica el último `TradeVolume` a la propiedad `Volume` de la clase base.
   - Maneja solicitudes de cierre o reversión manual incluso si aún no se han formado indicadores.
   - Una vez confirmados los datos de mercado como listos, ejecuta solicitudes de entrada pendientes, registra órdenes pendientes y evalúa disparadores de stop-loss / take-profit.
3. Cuando el tamaño de una posición cambia (nueva entrada, escalar entrada o reducción), la estrategia actualiza el precio de entrada almacenado para que los stops basados en desplazamiento reflejen inmediatamente la última operación.
4. La lógica de protección usa el máximo/mínimo de la vela para identificar violaciones. Cuando se cruza un nivel, se envía una orden de mercado en la dirección opuesta con el tamaño absoluto de la posición actual para asegurar que la posición quede completamente cerrada.

## Parámetros
- **`CandleType`** – serie de velas usada para monitorear precios en busca de desplazamientos y verificaciones de riesgo.
- **`TradeVolume`** – volumen aplicado a cada orden de mercado y pendiente (debe ser positivo).
- **`EntryAction`** – selector momentáneo con valores `None`, `BuyMarket` o `SellMarket`. Establecer un valor diferente de `None` dispara la orden de mercado correspondiente exactamente una vez y luego vuelve a `None`.
- **`PendingAction`** – selector de orden pendiente (`None`, `BuyLimit`, `BuyStop`, `SellLimit`, `SellStop`). La acción se consume después de que se registra una orden válida.
- **`PendingPrice`** – precio absoluto para la orden pendiente. Dejar en `0` para confiar en `PendingOffset`.
- **`PendingOffset`** – desplazamiento aplicado al cierre de vela más reciente cuando `PendingPrice` es cero. Los desplazamientos positivos ajustan automáticamente el precio por encima/debajo del cierre según la acción seleccionada.
- **`UseStopLoss`** / **`StopLossPrice`** / **`StopLossOffset`** – habilitar y configurar protección de stop-loss. Los desplazamientos se miden desde el precio de entrada almacenado cuando no se proporciona el precio absoluto.
- **`UseTakeProfit`** / **`TakeProfitPrice`** / **`TakeProfitOffset`** – configuraciones análogas para gestión de take-profit.
- **`ClosePositionRequest`** – establecer en `true` para emitir una salida de mercado inmediata para toda la posición. El indicador se restablece a `false` después de que se procesa la solicitud.
- **`ReversePositionRequest`** – establecer en `true` para invertir la exposición actual. La estrategia cierra la posición existente y abre una opuesta usando `ReverseVolume`, luego restablece el indicador.
- **`ReverseVolume`** – volumen de la nueva posición establecida después de una reversión. Si necesita que el tamaño inverso coincida con la posición existente, configúrelo igual a la posición absoluta actual.

## Directrices de Uso
1. Elija la agregación de velas (`CandleType`) que coincida con cómo desea medir desplazamientos y riesgo. El marco temporal predeterminado de 1 minuto refleja el comportamiento original del panel que reaccionaba a los ticks entrantes.
2. Configure `TradeVolume` y niveles de protección opcionales (`StopLoss*`, `TakeProfit*`). Puede cambiar libremente entre niveles absolutos y desplazamientos; los desplazamientos se activan siempre que el valor absoluto se deje en cero.
3. Para órdenes pendientes, decida si prefiere un precio fijo (`PendingPrice`) o un desplazamiento desde el último cierre (`PendingOffset`). La estrategia recalcula el precio en el momento en que se envía la orden.
4. Envíe instrucciones de operación cambiando `EntryAction`, `PendingAction`, `ClosePositionRequest` o `ReversePositionRequest`. Cada parámetro se comporta como un botón: una vez ejecutada la solicitud el valor se restablece automáticamente para que la acción no se repita en la siguiente vela.
5. La estrategia continúa monitoreando la acción del precio mientras una posición está abierta. Siempre que se cruza un umbral de stop-loss o take-profit la posición se cierra con una orden de mercado; ambos disparadores de protección se deshabilitan hasta la próxima entrada para evitar órdenes duplicadas.

## Diferencias de la Versión Original MQL
- El panel visual se reemplaza con parámetros de estrategia. Cada botón de la UI original ahora está expuesto como un interruptor o selector que puede editarse desde la cuadrícula de parámetros de StockSharp o scripts de automatización.
- En lugar de colocar órdenes stop o límite para protección, la estrategia cierra la posición con órdenes de mercado cuando se violan los niveles de precio especificados. Esto mantiene la implementación compatible con la API de alto nivel y evita mantener órdenes stop separadas.
- Los desplazamientos de precio usan velas terminadas en lugar de ticks sin procesar. Esto mantiene el comportamiento determinista en los backtests y sesiones de trading en vivo mientras sigue entregando capacidad de respuesta intradía.

## Notas
- Puede encolar múltiples instrucciones dentro de la misma vela (por ejemplo, solicitar una compra de mercado e inmediatamente solicitar un desplazamiento de take-profit). La estrategia los procesa secuencialmente en la siguiente vela terminada.
- Si necesita reemitir la misma acción, simplemente seleccione el valor deseado de nuevo; la lógica de seguimiento interno detecta el cambio y ejecuta la nueva solicitud.
- Al escalar hacia una posición, el precio de entrada almacenado se actualiza al cierre de la vela que refleja el nuevo tamaño. Ajuste los desplazamientos en consecuencia si requiere distancias de protección precisas.
