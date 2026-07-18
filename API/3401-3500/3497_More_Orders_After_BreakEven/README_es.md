# Más pedidos después del punto de equilibrio (puerto StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta carpeta contiene un puerto C# StockSharp del asesor experto MetaTrader 4 **"Más pedidos después del punto de equilibrio"** (MQL ID de fuente `35609`). El EA original agrega repetidamente nuevas posiciones largas una vez que las operaciones anteriores se han protegido hasta el punto de equilibrio. El puerto reproduce esa administración de dinero basada en tickets mientras se integra con el API de alto nivel de StockSharp.

## Descripción general de la estrategia

* **Lado del mercado** – solo posiciones largas. Cada operación es una orden de compra de mercado colocada sobre el valor principal de la estrategia.
* **Idea central**: si bien hay menos operaciones abiertas sin protección de equilibrio que `MaximumOrders`, la estrategia vuelve a comprar. Cuando una operación existente alcanza la distancia de equilibrio, su límite de pérdidas se eleva al precio de entrada para que ya no bloquee entradas adicionales.
* **Gestión de salida**: cada orden almacena sus propios niveles de límite de pérdidas y obtención de ganancias. Los stop se mueven al punto de equilibrio cuando el precio avanza `BreakEvenPips`. Las órdenes de venta del mercado cierran posiciones cuando el precio de oferta toca cualquiera de los niveles de protección.
* **Procesamiento de ticks**: el EA original funcionó en cada tick a través de `OnTick`. El puerto utiliza datos de mercado de nivel 1 para monitorear los mejores precios de oferta y demanda y emula el mismo comportamiento: cada actualización evalúa las entradas, las reglas de equilibrio y las posibles salidas.

## Parámetros

| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `MaximumOrders` | Número máximo de operaciones largas cuyo stop-loss aún no ha alcanzado el punto de equilibrio. Una vez que el recuento cae por debajo de este umbral, se pueden abrir nuevas posiciones. | `1` |
| `TakeProfitPips` | Distancia desde el precio de entrada hasta el objetivo de obtención de beneficios expresada en MetaTrader pips. Un valor de `0` deshabilita la toma de ganancias. | `100` |
| `StopLossPips` | Distancia inicial hasta la parada de protección en MetaTrader pips. Establezca en `0` para abandonar la posición sin una parada inicial (la regla de equilibrio aún puede protegerla más adelante). | `200` |
| `BreakEvenPips` | Distancia de beneficio (en MetaTrader pips) después de la cual el stop-loss se eleva al precio de entrada. `0` significa que el stop pasa al punto de equilibrio tan pronto como el precio excede el precio de entrada. | `10` |
| `TradeVolume` | Volumen enviado con cada orden de compra de mercado. | `0.01` |
| `DebugMode` | Cuando está habilitada, la estrategia registra mensajes informativos que imitan la salida `Comment()` del EA original. | `true` |

Todas las distancias basadas en pips se adaptan automáticamente a símbolos forex de 4/2 y 5/3 dígitos analizando el tamaño del tick y la precisión decimal del instrumento, replicando el factor de escala `points` del código original.

## Lógica de trading

1. **Suscripción de nivel 1**: la estrategia se suscribe a las mejores cotizaciones de oferta y demanda. Cada vez que se conocen ambos precios, `ProcessPrices` emula el bucle MQL `OnTick`.
2. **Recuento de pedidos**: antes de realizar un nuevo pedido, la estrategia cuenta las entradas abiertas que aún no han alcanzado el punto de equilibrio. Esto reproduce el ayudante `OrdersCounter()` original.
3. **Entradas**: cuando el recuento es inferior a `MaximumOrders`, se envía una nueva orden de compra de mercado usando `TradeVolume`. Se registra el precio de llenado y se inicializan los niveles de parada/toma de ganancias por boleto.
4. **Actualización del punto de equilibrio**: para cada entrada activa, el precio de oferta se compara con el disparador del punto de equilibrio. Una vez superado, el stop-loss se traslada al precio de entrada, marcando el billete como protegido para que ya no contribuya al recuento de la orden.
5. **Comprobaciones de salida**: el precio de oferta también impulsa la detección de salida. Si alcanza la toma de ganancias almacenada o cae hasta el límite de pérdidas (incluido el límite de equilibrio), la estrategia emite una orden de venta de mercado para el volumen restante de ese billete.
6. **Seguimiento de posición**: los llenados recibidos a través de `OnOwnTradeReceived` mantienen una lista FIFO de entradas. Esto reproduce el comportamiento del ticket de MetaTrader, donde cada orden se puede manejar individualmente aunque StockSharp agregue la posición neta.

## Diferencias con el original EA

* Solo se implementan operaciones largas porque la versión MQL nunca emitió entradas de venta.
* Las órdenes de stop y toma de ganancias del corredor se reemplazan con monitoreo del lado de la estrategia y salidas del mercado. Esto es necesario porque StockSharp no modifica automáticamente las paradas por pedido en el nivel alto API.
* La salida de diagnóstico utiliza el sistema de registro de StockSharp en lugar del texto de `Comment()` en el gráfico MetaTrader.

## Notas de uso

1. Adjunte la estrategia a un conector que proporcione datos de nivel 1 para la seguridad elegida.
2. Configure los parámetros basados en pips para que coincidan con la volatilidad del instrumento y los requisitos del corredor.
3. Habilite `DebugMode` durante las pruebas para verificar el recuento de pedidos y el comportamiento de equilibrio, luego desactívelo en producción para obtener registros más silenciosos.
4. Dado que las salidas se manejan mediante órdenes de mercado, asegúrese de que la cartera tenga suficiente poder adquisitivo disponible para cubrir todas las entradas adicionales que puedan activarse una vez que entre en acción la protección del punto de equilibrio.

## Referencia fuente

* Archivo MQL4 original: `MQL/35609/More Orders After BreakEven.mq4`.
* Estrategia C# convertida: `CS/MoreOrdersAfterBreakEvenStrategy.cs`.
