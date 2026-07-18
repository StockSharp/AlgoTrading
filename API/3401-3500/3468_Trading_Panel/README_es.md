# Estrategia del panel comercial (ID 3468)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
**TradingPanelStrategy** es un asistente de entrada de órdenes manual convertido del asesor experto MQL5 *EA_TradingPanel*. Expone métodos programáticos que replican el panel original en el gráfico: una sola acción puede enviar múltiples órdenes de mercado, adjuntar automáticamente distancias de stop-loss y take-profit medidas en pips y, opcionalmente, seleccionar un valor personalizado para negociar. Los valores predeterminados reflejan la fuente EA (una operación, parada de 2 pips, toma de 10 pips, volumen de 0,01).

A diferencia del panel gráfico, este puerto StockSharp se centra en puntos de entrada fáciles de automatizar. Las personas que llaman (por ejemplo, una interfaz de usuario o un script personalizado) pueden activar `PlaceBuyOrders()` o `PlaceSellOrders()` cuando sea necesario, mientras que la estrategia se encarga de la normalización del volumen, el redondeo de precios y la colocación de órdenes de protección.

## Parámetros
| Nombre | Descripción | Notas |
| ---- | ----------- | ----- |
| `TradeCount` | Número de órdenes de mercado enviadas por acción. | Garantiza al menos cero. Predeterminado `1`. |
| `StopLossPips` | Distancia de stop-loss en pips. | Zero desactiva la creación de paradas. Predeterminado `2`. |
| `TakeProfitPips` | Distancia de toma de ganancias en pips. | Zero desactiva la creación de objetivos. Predeterminado `10`. |
| `VolumePerTrade` | Volumen para cada orden de mercado individual. | Redondeado vía `Security.VolumeStep`. Predeterminado `0.01`. |
| `TargetSecurity` | Anulación opcional para el instrumento negociado. | Vuelve a `Strategy.Security` cuando es nulo. |

Todos los parámetros se exponen a través de `StrategyParam<T>` para que admitan la optimización y la reconfiguración del tiempo de ejecución desde la interfaz de usuario de StockSharp.

## Flujo de ejecución
1. Resolver la seguridad activa (`TargetSecurity` o `Strategy.Security`).
2. Derive el tamaño del pip a partir de los metadatos del instrumento: `PriceStep` multiplicado por 10 cuando el instrumento tiene más de 3 decimales, idéntica a la lógica MQL que multiplica por símbolos con 3 o 5 dígitos.
3. Obtenga el último precio de referencia (mejor oferta/demanda, volviendo a la última operación) y redondeelo con `Security.ShrinkPrice`.
4. Calcule el volumen deseado: `TradeCount × VolumePerTrade`, alinéelo con los límites de intercambio (`MinVolume`, `MaxVolume`, `VolumeStep`) y ajústelo para una posición abierta opuesta para que una acción pueda aplanarse y revertirse.
5. Envíe una orden de mercado a través de `BuyMarket` o `SellMarket`.
6. Cree órdenes de protección (detención y límite) utilizando las compensaciones de pips, nuevamente normalizadas al tamaño del tick del intercambio.
7. Cancele las órdenes de protección obsoletas cada vez que la posición cambie o la estrategia se detenga.

## Lógica de la orden de protección
- Las entradas largas colocan un `SellStop` para el stop-loss y un `SellLimit` para el take-profit.
- Las entradas cortas colocan un `BuyStop` para el stop-loss y un `BuyLimit` para el take-profit.
- Cada orden de protección cubre el volumen del panel recién solicitado (la misma cantidad que una sola acción en el panel MQL original).
- Los pedidos se cancelan automáticamente en `OnStopped`, `OnReseted` y siempre que se active el lado opuesto.

## Notas de uso
- Asigne `Strategy.Security` en la aplicación host o proporcione un `TargetSecurity` antes de llamar a los métodos del panel; de lo contrario no se enviarán transacciones.
- Invoque `PlaceBuyOrders()` para replicar el botón MQL "COMPRAR" y `PlaceSellOrders()` para el botón "VENDER".
- Los precios se basan en datos del mercado en vivo. Si no están disponibles ni la mejor oferta/demanda ni la última operación, la estrategia registra un error y omite el envío de la orden.
- El asistente llama a `StartProtection()` en `OnStarted` para protegerse contra posiciones obsoletas después de los reinicios.
- Cuando los metadatos del instrumento no incluyen `PriceStep`, el tamaño del pip por defecto es `0.0001` (un pip para la mayoría de los símbolos FX); establezca `PriceStep` explícitamente si su corredor utiliza incrementos alternativos.

## Diferencias en comparación con el panel MQL
- No hay una interfaz de usuario gráfica integrada. Se espera que los integradores creen su propia interfaz o activen los métodos públicos desde la lógica externa.
- Las órdenes de protección se agregan por acción en lugar de por ticket MT5 individual. La exposición neta resultante coincide con el comportamiento de MT5 y al mismo tiempo mantiene concisa la implementación de StockSharp.
- La validación de volumen y precio sigue las convenciones StockSharp (`Security.ShrinkPrice`, `VolumeStep`, `MinVolume`, `MaxVolume`). Esto evita pedidos rechazados en lugares con incrementos estrictos.
- El registro de ejecución se proporciona a través de `LogInfo` y `LogError` para ayudar al monitoreo en las terminales StockSharp.

## Empezando
1. Cree una instancia de la estrategia, asigne cartera y seguridad (o establezca `TargetSecurity`).
2. Iniciar la estrategia para que `StartProtection()` arme las salvaguardas internas.
3. Llame a `PlaceBuyOrders()` o `PlaceSellOrders()` según la entrada del usuario o los activadores automáticos.
4. Supervise el registro en busca de mensajes de confirmación y administre la lógica de interfaz de usuario adicional según sea necesario.

Esta conversión manual del panel de operaciones ofrece una reproducción ligera pero fiel del asesor experto MT5 original, adaptado al marco estratégico de alto nivel de StockSharp.
