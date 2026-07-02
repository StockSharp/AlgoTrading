# Para Max V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Para Max V2 es un puerto del MetaTrader 4 asesor experto `for_max_v2.mq4`. La estrategia espera patrones envolventes específicos de dos velas y luego coloca un par simétrico de órdenes buy-stop y sell-stop alrededor de la vela más reciente. Una vez que se completa una orden de ruptura, se elimina la orden pendiente opuesta y la posición se administra con paradas fijas, niveles de obtención de ganancias opcionales y una rutina de seguimiento que primero bloquea una pequeña ganancia en el punto de equilibrio y luego sigue el precio.

## Lógica estratégica
### Detección de patrones envolventes
El perito original expone dos bloques de entrada y ambos se conservan:
* **Configuración de tipo 1**: escanea las velas `Max Search` anteriores (omitiendo la barra actual) y espera a que el mínimo más bajo dentro de ese rango ocurra hace dos barras **o** que el máximo más alto ocurra hace dos barras. Cuando eso sucede, la vela dos barras atrás debe engullir la vela anterior (máximo más alto y mínimo más bajo). La configuración se coloca alrededor de la vela terminada más reciente.
* **Configuración de tipo 2**: también escanea las velas `Max Search` anteriores, pero busca que el extremo aparezca una barra atrás. Además, la vela que está una barra atrás debe envolver a la vela dos barras atrás. Luego se coloca una horquilla alrededor de la vela más reciente. Ambas configuraciones pueden coexistir; cada uno gestiona sus propias órdenes pendientes y su reloj de vencimiento.

### Pendiente de realizar el pedido
* **Precios de entrada**: las órdenes de compra se colocan en el máximo de la vela anterior más `Gap Points`, las órdenes de venta en el mínimo de la vela anterior menos `Gap Points`.
* **Stop-loss**: para el Tipo 1, el stop largo está anclado en el mínimo de la vela dos barras atrás (menos el hueco) y el stop corto en el máximo de esa vela (más el hueco). El tipo 2 utiliza la vela anterior para ambos lados.
* **Take-profit** – opcional. Los objetivos largos suman `Gap Points + Buy Take Profit Points` al máximo anterior y los cortos restan `Gap Points + Sell Take Profit Points` del mínimo anterior. Establecer las entradas de obtención de beneficios en `0` deshabilita los objetivos respectivos.
* **Vencimiento**: cada combinación lleva una marca de tiempo de validez calculada como `Order Expiry (bars)` multiplicada por el período de tiempo de vela configurado. Si las órdenes pendientes todavía están funcionando cuando se alcanza la marca de tiempo, ambas partes se cancelan.

### Gestión de posiciones
* Una vez que se completa un stop de compra, se cancelan todas las órdenes de stop de venta restantes de cualquiera de las configuraciones; la regla simétrica se aplica después de una entrada breve.
* Las paradas y los objetivos se controlan en las velas completadas. Si el mínimo de una vela alcanza el stop largo (o el máximo alcanza el stop corto), la posición se cierra con una orden de mercado. Se utiliza el mismo enfoque para los niveles de obtención de beneficios.
* La rutina de equilibrio (`Break-even Trigger` y `Break-even Offset`) mueve el stop al precio de entrada más/menos el desplazamiento configurado una vez que la posición avanza por el monto de activación.
* El bloque final mantiene los puntos de parada `Long/Short Trailing Buffer` alejados de la mejor excursión, pero solo después de que el precio haya viajado lo suficiente (y opcionalmente solo después de que la operación ya sea rentable). `Trailing Step` evita ajustes demasiado frecuentes al requerir una mejora mínima antes de apretar nuevamente el tope.

## Parámetros
* **Volumen**: volumen de orden para cada orden de parada pendiente.
* **Comprar Take Profit (puntos)**: distancia en puntos utilizada para calcular el take-profit largo (establecido en `0` para desactivarlo).
* **Vender Take Profit (puntos)**: distancia en puntos utilizada para calcular la toma de ganancias corta (establecida en `0` para desactivarla).
* **Gap (puntos)** – buffer agregado a máximos/mínimos antes de colocar entradas de stop y plegado en la distancia de toma de ganancias.
* **Profundidad de búsqueda**: número de velas terminadas escaneadas al verificar configuraciones envolventes de Tipo 1 y Tipo 2.
* **Vencimiento de la orden (barras)**: número de longitudes de vela que una combinación pendiente permanece activa antes de que se cancelen ambos lados.
* **Activador de equilibrio (puntos)**: umbral de beneficio que activa el ajuste del punto de equilibrio.
* **Compensación del punto de equilibrio (puntos)**: colchón adicional que se agrega al precio de entrada cuando se coloca el punto de equilibrio.
* **Buffer de seguimiento largo (puntos)**: distancia de seguimiento para posiciones largas una vez que se ha alcanzado el punto de equilibrio.
* **Búfer de seguimiento corto (puntos)**: distancia de seguimiento para posiciones cortas una vez que se ha alcanzado el punto de equilibrio.
* **Paso de seguimiento (puntos)**: se requiere una mejora mínima en la ubicación de la parada antes de actualizar la parada de seguimiento nuevamente.
* **Seguro solo después de la ganancia**: si está habilitado, el seguimiento espera hasta que la posición se haya movido más allá del buffer antes de activarse.
* **Tipo de vela**: período de tiempo de las velas utilizadas para la detección de patrones, el vencimiento de la orden y el procesamiento de salida.

## Notas adicionales
* Las compensaciones de precios expresadas en “puntos” dependen del valor `PriceStep`. Los símbolos con cinco (o tres) decimales se convierten automáticamente a tamaños de pips fraccionarios como en MetaTrader.
* Stop Loss y Take Profits se ejecutan a través de órdenes de mercado dentro de la estrategia para reflejar el comportamiento de EA de gestionar niveles en velas cerradas.
* La estrategia no implementa la función `vhod_3` no utilizada de la fuente original; sólo se trasladaron los dos bloques de entrada activos.
* Este paquete contiene sólo la implementación de C#; no se proporciona ninguna versión de Python.
