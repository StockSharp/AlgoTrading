# Estrategia sencilla
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Simple** es la StockSharp conversión de alto nivel del MetaTrader 4 asesores expertos `S!mple.mq4` ubicado en `MQL/9019`. El sistema original monitoreaba una canasta fija de símbolos Forex y operaba cada vez que una media móvil ponderada lineal de 50 períodos cruzaba una media móvil simple de 200 períodos. Cada entrada se podía repetir un número configurable de veces y se adjuntaban a cada operación niveles opcionales de stop-loss y take-profit basados ​​en dinero. La conversión mantiene la misma lógica, expone todas las entradas del usuario como parámetros de estrategia y registra la misma información de diagnóstico que EA imprimió en el comentario del terminal MetaTrader.

## Lógica de trading
1. **Preparación de datos.** La estrategia se suscribe a un tipo de vela configurable (velas de cinco minutos de forma predeterminada) y vincula ambas medias móviles a través del nivel alto `SubscribeCandles().Bind(...)` API.
2. **Cruce de media móvil.** Se almacenan dos valores históricos de cada media móvil. Se produce una señal de compra cuando la LWMA rápida estuvo por debajo de la lenta SMA hace dos barras y cerró por encima de ella en la barra finalizada anterior. Se detecta una señal de venta cuando ocurre la condición inversa.
3. **Seguimiento del margen de tendencia.** El valor lento de SMA que ocurrió hace `TrendMargin` barras se almacena en caché para reproducir el informe de tendencia textual de EA. La velocidad lenta en vivo SMA se compara con esa referencia para clasificar la tendencia de fondo como `UP`, `DOWN` o `WAIT`, junto con la distancia expresada en incrementos de precios.
4. **Modelo de ejecución.**
   - Cuando se activa una señal de compra, cualquier exposición corta se cierra antes de comprar hasta `NumOrders * TradeVolume`. El volumen solicitado refleja el comportamiento de EA en el que se acumularon varios pedidos idénticos hasta alcanzar el recuento máximo.
   - Una señal de venta cierra primero la exposición larga y luego vende hasta el mismo volumen objetivo agregado.
5. **Niveles de protección.** Las paradas y objetivos opcionales basados en dinero (`StopLossMoney`, `TakeProfitMoney`) se traducen en distancias de precios utilizando el instrumento `PriceStep`/`StepPrice` y por orden `TradeVolume`. Una vez que se almacenan los niveles, cada vela terminada verifica el rango alto/bajo; si se supera un nivel, la posición se nivela en el mercado.
6. **Guardia operativa.** La colocación de la orden real se ejecuta solo cuando `EnableTrading` está configurado en `true`, replicando el indicador original `makeTrades` que permitía que EA se ejecutara en modo "solo análisis".

## Gestión de riesgos y paradas de dinero
- Los importes de stop-loss y take-profit se interpretan como riesgo/objetivo de efectivo por bloque de entrada (por orden MetaTrader). La conversión utiliza los metadatos de seguridad (`PriceStep`, `StepPrice`) para convertir esa cantidad en un número redondeado de pasos de precio. Si falta alguno de los campos, se registra una advertencia y las paradas monetarias permanecen deshabilitadas.
- Los niveles de protección se evalúan en el máximo/mínimo de cada vela completa, coincidiendo con las comprobaciones de nivel de tick realizadas por EA mientras se mantienen dentro del marco de alto nivel de StockSharp.
- `StartProtection()` se invoca al inicio para que las protecciones a nivel de cuenta configuradas en StockSharp permanezcan activas mientras se ejecuta la estrategia.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volumen de un único pedido similar a MetaTrader. La base `Strategy.Volume` se mantiene sincronizada con este valor. |
| `NumOrders` | `1` | Número máximo de bloques de volumen que se pueden acumular en la misma dirección. El volumen objetivo final es igual a `TradeVolume * NumOrders`. |
| `StopLossMoney` | `0` | Monto de límite de pérdidas opcional en la moneda de la cuenta por bloque de volumen. Establezca en cero para desactivar la parada. |
| `TakeProfitMoney` | `0` | Monto de obtención de ganancias opcional en la moneda de la cuenta por bloque de volumen. Establezca en cero para desactivar el objetivo. |
| `TrendMargin` | `10` | Número de velas terminadas utilizadas para producir el texto de tendencia de fondo (lento SMA en comparación con su valor hace `TrendMargin` barras). |
| `FastLength` | `50` | Longitud de la media móvil ponderada lineal rápida. |
| `SlowLength` | `200` | Longitud de la media móvil simple lenta. |
| `EnableTrading` | `false` | Cuando `false` la estrategia solo registra señales, exactamente como EA cuando `makeTrades=false`. |
| `CandleType` | `5m time-frame` | Tipo de vela utilizado para los cálculos del indicador. |

## Notas sobre la conversión
- El MetaTrader EA recorrió seis símbolos Forex codificados. Las estrategias StockSharp operan sobre el `Strategy.Security` proporcionado por el usuario. Para reproducir el comportamiento de negociación de cestas, lance varias instancias de la estrategia (una por instrumento) o envuélvalas dentro de una estrategia principal que envíe las mismas señales a múltiples valores.
- Los niveles de protección basados en dinero dependen de los metadatos del instrumento. Para pares de Forex, asegúrese de que tanto `PriceStep` como `StepPrice` estén completos (por ejemplo, `0.0001` y el valor del pip por lote). De lo contrario, la distancia de parada/objetivo se trata silenciosamente como cero después de registrar una advertencia.
- El mensaje de registro emitido en cada vela terminada refleja el comentario EA: enumera la señal (`BUY`, `SELL` o `WAIT`), ambos promedios móviles, la distancia entre ellos en pasos de precios y la evaluación de la tendencia obtenida del lento retrasado SMA.
- El número de órdenes acumuladas se modela como un volumen objetivo agregado. Esto mantiene la exposición total idéntica a la implementación original mientras se utilizan los asistentes de órdenes de mercado de alto nivel de StockSharp en lugar de múltiples llamadas individuales de `OrderSend`.
- Aún no se ha creado ningún puerto Python que coincida con los requisitos de la tarea.

## Consejos de uso
- Asigne un valor de Forex con los valores `PriceStep`, `StepPrice` y `VolumeStep` configurados correctamente. Establezca `TradeVolume` en el tamaño de lote que desee y habilite la negociación una vez que esté satisfecho con los diagnósticos registrados.
- Para imitar el comportamiento predeterminado de EA (solo análisis), deje `EnableTrading` en `false`. Cuando esté listo para operar, gírelo a `true` y la siguiente señal cruzada enviará órdenes de mercado.
- Debido a que los niveles de protección se monitorean al cerrar las velas, considere usar velas más cortas si necesita una reacción intrabar más estricta en comparación con el comportamiento tick a tick de MetaTrader.
