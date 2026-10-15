# Estrategia mínima de Frank Ud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este ejemplo traslada el asesor experto clásico **Frank Ud** MetaTrader a StockSharp utilizando la estrategia de alto nivel API. El script original MQL ejecuta una cuadrícula de martingala cubierta que sigue agregando posiciones cada vez que el precio se mueve con respecto a la última entrada. Las ganancias se bloquean una vez que la orden más reciente (y por lo tanto la más grande) gana una cantidad fija de pips, después de lo cual *todas* las operaciones de ese lado se cierran simultáneamente.

## Lógica central

1. **Cobertura simétrica.** La estrategia mantiene dos escaleras independientes de posiciones de mercado: una escalera larga y una escalera corta. Por lo tanto, es posible mantener posiciones largas y cortas al mismo tiempo, como en el modo de cobertura de MetaTrader.
2. **Martingale progresión.** El primer pedido en cualquier lado usa `InitialVolume` (por defecto, 0,1 lotes). Cada entrada posterior en el mismo lado duplica el mayor volumen abierto actualmente. Todo lote que la estrategia envía —incluido el primero— se ajusta después a lo que el instrumento admite realmente: se redondea a la baja hasta un número entero de unidades de `VolumeStep`, se eleva a `MinVolume` si queda por debajo y se limita a `MaxVolume`. Las restricciones que el instrumento no informa se omiten.
3. **Espaciado de entrada.** Se agrega una nueva posición solo cuando el precio se ha movido al menos `ReEntryPips` (predeterminado 41 pips) más allá del mejor precio de entrada de la escalera existente. La escalera larga espera a que los precios de venta caigan por debajo de `lowest_buy - ReEntryPips`, mientras que la escalera corta espera a que los precios de oferta suban por encima de `highest_sell + ReEntryPips`. Ambos lados de la cotización se toman del cierre de la misma vela, de modo que en esta portación las dos comparaciones se hacen contra el mismo precio.
4. **Recolección de ganancias.** Para cada escalera, la operación con el mayor volumen actúa como la orden "desencadenante". Cuando su beneficio excede `TakeProfitPips` (65 pips predeterminado), o cuando el precio alcanza el objetivo con margen situado a `TakeProfitPips + ExtraTakeProfitPips` pips de esa entrada, cada posición en ese lado se aplana con una única orden de mercado y la escalera se vacía.
5. **Protección de margen.** Antes de enviar una nueva entrada, la estrategia verifica que el margen libre de la cartera —su valor actual menos la comisión que informa— se mantenga por encima de `Balance × MinimumFreeMarginRatio` (predeterminado 0,5). La protección cubre ambas escaleras y todas sus entradas, incluida la primera. Fijar la relación en cero la desactiva, y lo mismo ocurre si la cartera no devuelve valor alguno: en ambos casos la verificación simplemente se supera y la estrategia vuelve al comportamiento de volumen fijo del experto original.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `TakeProfitPips` | Umbral de beneficio de pip medido en el pedido más grande y más reciente. Una vez superado, se cierran todas las posiciones de ese lado. |
| `ReEntryPips` | Distancia mínima de pips entre la mejor entrada existente y la oferta/demanda actual antes de que se agregue una nueva orden de martingala. |
| `InitialVolume` | Tamaño de lote base para el primer pedido de cada escalera. Los pedidos posteriores duplican el mayor volumen activo. |
| `MinimumFreeMarginRatio` | Relación requerida entre margen libre y saldo antes de que se permitan nuevas entradas. Establezca en 0 para desactivar la verificación. Valor predeterminado 0,5. |
| `ExtraTakeProfitPips` | Distancia adicional en pips que se suma a `TakeProfitPips` al calcular el objetivo de salida con margen. Valor predeterminado 25. |
| `CandleType` | Serie de velas a la que se suscribe la estrategia. Valor predeterminado: marco temporal de 1 minuto. |

## Notas de implementación

- Un pip no es el paso de precio en bruto. En la primera vela cerrada que procesa, la estrategia fija un pip en una diezmilésima parte del precio cotizado, lo limita por abajo al paso de precio del instrumento (para que nunca sea más fino de lo que el instrumento realmente negocia) y después conserva ese valor durante el resto de la ejecución, de modo que la cuadrícula no se desplace bajo sus propios pies. Así se reproduce la convención de forex para la que se escribió el experto (0,0001 en EURUSD a 1,10; 0,01 en USDJPY a 150) y las distancias siguen siendo significativas en un instrumento cotizado con cinco cifras, donde el paso en bruto de 0,01 alcanzaría un objetivo de 65 pips en casi cada vela. Si el instrumento no informa un paso de precio, el pip queda definido únicamente por esa fracción.
- La estrategia se basa en velas cerradas, no en cotizaciones de nivel 1. Se suscribe a la serie `CandleType` (marco temporal de 1 minuto de forma predeterminada) e ignora toda vela que aún no esté cerrada. El historial incluido no trae libro de órdenes, por lo que el cierre de la vela cerrada hace las veces tanto de oferta como de demanda. Las implementaciones en C# y en Python se suscriben exactamente de la misma manera.
- La entrada en la escalera se registra en el momento en que se envía la orden, no cuando se ejecuta: al abrir se añaden a la lista el cierre de la vela y el volumen solicitado, y al cerrar se envía una única orden de mercado por todo el volumen de la escalera y la lista se vacía. No se mantiene ningún diccionario de intenciones de órdenes ni se usa una devolución de llamada de ejecución: en este emulador la ejecución llega de forma síncrona dentro del registro de la orden, antes de que la orden pudiera siquiera escribirse en semejante diccionario.
- La contabilidad de posiciones almacena cada entrada de la escalera (precio y volumen) en listas simples en lugar de consultar estadísticas acumulativas, preservando el comportamiento de las matrices MQL que se utilizaron para localizar el lote más grande y su precio de entrada.
- El buffer adicional en pips que el experto original colocó en cada orden de toma de ganancias se expone como el parámetro `ExtraTakeProfitPips` (25 pips de forma predeterminada) y se retiene como condición de salida adicional.

> Las implementaciones están disponibles en C# y Python.
