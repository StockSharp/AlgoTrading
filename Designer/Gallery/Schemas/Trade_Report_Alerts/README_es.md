# Diagrama de la estrategia Trade Report Alerts
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama es un ejemplo de generación de informes más que de invención de señales. Un simple cruce de una media móvil exponencial de 9 periodos y otra de 26 sobre velas de cinco minutos finalizadas aporta las operaciones, y todo lo que lo rodea convierte esas operaciones en texto legible: cada ejecución propia se convierte en una línea de log en el momento en que se produce, y una vez al día una rama gobernada por el reloj escribe el resultado realizado de la estrategia. La parte de informes lee la parte de negociación y nunca coloca, modifica ni bloquea una orden propia.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cinco minutos finalizadas alimentan una ExponentialMovingAverage rápida de 9 y otra lenta de 26. El bloque Crossing reduce el par a un único evento: `true` cuando la línea rápida cruza por encima de la lenta, `false` cuando cruza por debajo, y nada en absoluto entre medias.
- La posición se lee una vez por vela mediante una instantánea disparada por la vela, y tres comparaciones contra cero la describen como plana, larga o corta. Cada decisión se construye a partir de esa instantánea, de modo que una ejecución que llegue en mitad de la barra no puede reabrir una decisión ya tomada.
- Desde una posición plana, un cruce al alza abre una posición larga y un cruce a la baja abre una corta. Ambos bloques de entrada llevan la condición Open-position, por lo que permanecen en silencio mientras se mantenga cualquier posición y no pueden acumular orden sobre orden.
- Desde una posición abierta, el cruce contrario la cierra mediante un bloque Close-position, que dimensiona la orden a partir de la propia posición. El evento de orden de ese cierre dispara entonces la entrada en la nueva dirección, de modo que una reversión se escribe como dos pasos explícitos en lugar de una única orden sobredimensionada.
- Un único valor Volume expuesto alimenta los cuatro bloques de entrada; los dos bloques de cierre no reciben volumen, porque un bloque Close-position ya sabe cuánto hay abierto.
- El bloque Strategy trades recoge cada ejecución propia de la estrategia y la envía a través de un String formatter hacia una notificación de tipo Log, de modo que cada ejecución deja una línea con el sentido, la cantidad, el instrumento y el precio.
- Una segunda rama informa según el reloj en lugar de según el mercado. Current time alimenta dos ventanas Working time —una ventana de informe alrededor del mediodía y una ventana de reinicio justo después de medianoche— y un Flag situado entre ellas convierte toda la ventana de informe en exactamente un pulso al día.
- Ese único pulso libera el resultado realizado de la estrategia desde una variable que conserva el último valor recibido, lo formatea y lo escribe en el log. Como la variable parte de cero, sigue apareciendo una línea de estado en un día que no haya producido ninguna ejecución.

## Reglas de entrada y salida

- **Entrada en largo**: Un cruce al alza de la media exponencial rápida sobre la lenta, evaluado mientras la instantánea tomada en el momento de la vela muestra una posición plana, envía una compra a mercado por Volume. Si en cambio hay una posición corta abierta, el mismo cruce la cierra primero por completo, y la orden de cierre resultante dispara de inmediato la entrada larga, de modo que la dirección cambia dentro de la misma vela.
- **Entrada en corto**: Un cruce a la baja de la media exponencial rápida por debajo de la lenta, evaluado mientras la instantánea tomada en el momento de la vela muestra una posición plana, envía una venta a mercado por Volume. Si en cambio hay una posición larga abierta, el mismo cruce la cierra primero por completo, y la orden de cierre resultante dispara de inmediato la entrada corta.
- **Salida**: No hay bloque de stop-loss, de take-profit ni de protección: una posición se mantiene hasta el cruce contrario, que la cierra por completo mediante un bloque Close-position cuyo volumen se deriva de la posición. La rama de informes observa las ejecuciones y el beneficio, y nunca emite, reemplaza ni cancela una orden, por lo que desactivar las notificaciones dejaría el comportamiento de negociación sin cambios.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de la serie de velas; solo las velas finalizadas mueven los indicadores y todas las decisiones construidas sobre ellos. |
| Fast EMA Length | 9 | Periodo de la ExponentialMovingAverage rápida; es la mitad rápida del par de cruce. |
| Slow EMA Length | 26 | Periodo de la ExponentialMovingAverage lenta; es la mitad lenta del par de cruce. |
| Volume | 1 | Cantidad suministrada a los cuatro bloques de entrada. Los dos bloques de cierre la ignoran y toman su tamaño de la posición abierta. |
| Report Window Begin | 12:00:00 | Inicio de la ventana diaria de informe. El primer instante dentro de ella genera el informe de estado. |
| Report Window End | 12:05:00 | Fin de la ventana diaria de informe. Solo tiene que ser lo bastante amplia para que el reloj caiga dentro de ella una vez; el flag mantiene el informe en una sola línea sea cual sea su anchura. |
| Day Reset Begin | 00:00:00 | Inicio de la ventana de reinicio que borra el flag y permite un nuevo informe al día siguiente. |
| Day Reset End | 00:05:00 | Fin de la ventana de reinicio. Entre este instante y el inicio de la ventana de informe la rama permanece en silencio. |
| Fill Report Caption | Trade report | Título escrito en cada notificación de ejecución, que es como se reconocen en el log las líneas por operación. |
| Status Report Caption | Strategy status | Título escrito en la notificación de estado diario, que la separa de las líneas por operación. |

## Detalles del diagrama

- El bloque [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite únicamente velas de cinco minutos finalizadas, y dos bloques [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) calculan sobre ellas valores de ExponentialMovingAverage de 9 y 26. El filtrado por indicador formado está desactivado, por lo que ambas líneas están disponibles desde el inicio de la reproducción y ambas se dibujan en el gráfico.
- El bloque [Crossing](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) se dispara solo en un cruce; una [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) NOT convierte su evento a la baja en un disparador positivo. La [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) actual se almacena en una [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) liberada una vez por vela, y tres bloques [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) la convierten en indicadores de plana, larga y corta que cuatro condiciones AND combinan con el cruce.
- Seis bloques [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) actúan sobre esas cuatro condiciones: dos entradas desde posición plana con la condición `OpenPosition`, dos cierres con la condición `ClosePosition`, y dos entradas `OpenPosition` adicionales disparadas por el evento de orden del cierre correspondiente, que es lo que hace que una reversión suceda en dos pasos. Los seis colocan órdenes a mercado y ninguno de ellos espera una conexión en línea.
- [Strategy trades](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html) emite cada ejecución propia. Un [String Formatter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) la representa con la plantilla `Fill: {Order.Side} {Trade.TradeVolume:0.########} {Order.Security.Id} @ {Trade.TradePrice:0.########}`, y una [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) de tipo `Log` la escribe bajo el título del informe de ejecuciones.
- [Current time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) alimenta dos comprobaciones [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html); la ventana de informe activa un [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) y la ventana de reinicio lo borra, que es lo que limita la rama a un pulso al día. El pulso libera el valor realizado del bloque [P&L](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) desde una variable preajustada a cero, un segundo String Formatter escribe `Daily status: realized result {0}`, y una segunda notificación `Log` lo publica.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
