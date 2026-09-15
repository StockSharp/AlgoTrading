# Diagrama de la estrategia Twenty Pips Once a Day
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama abre como máximo una posición contratendencia al día. Una vez al día, a una hora determinada del reloj y solo mientras la cuenta está plana, compara el cierre de la vela horaria terminada con el cierre de la vela de 29 barras antes y apuesta contra la deriva de esa ventana: compra tras una caída y vende tras una subida. Un take-profit pequeño, un stop más amplio y un límite estricto de antigüedad de la posición se encargan de la salida.

![schema](schema.svg)

## Resumen de la estrategia

- Todo lo gobiernan las velas horarias terminadas. Nada se evalúa dentro de una barra en formación, así que cada decisión se toma sobre un precio ya cerrado.
- Previous value guarda la vela de 29 barras atrás. Su cierre se compara con el cierre actual, lo que mide la deriva de aproximadamente el último día y cuarto.
- La comparación decide el lado contrario a esa deriva: un cierre antiguo por encima del actual significa que el mercado cayó y el esquema compra; un cierre antiguo por debajo significa que el mercado subió y el esquema vende. Se usan dos comparaciones estrictas, de modo que una ventana que termina exactamente donde empezó no genera ninguna señal.
- Time aporta la lectura del reloj que acompaña a la vela recién cerrada, un convertidor toma su hora y una comparación con el parámetro Trading Hour abre la ventana de entrada durante una vela al día.
- La posición actual debe estar plana. Junto con el filtro horario de una vez al día y la condición Open position de los bloques de entrada, esto es lo que mantiene el esquema en una sola posición a la vez.
- Ambas entradas son órdenes a mercado de volumen fijo. Sus ejecuciones se unen y se entregan a Position protection, que cierra la posición con un take-profit del 0.1% o un stop-loss del 0.5%, la misma proporción de uno a cinco sobre la que se construye la idea.
- Un contador N values se arma con la entrada aceptada y cuenta 21 velas terminadas. Cuando se agota, un bloque Position modify configurado como Close position cierra lo que siga abierto, de modo que una posición que no alcanzó ninguno de los dos objetivos no se arrastra indefinidamente.
- Is trade allowed vigila el permiso de negociación en vivo de la plataforma. En cada entrada aceptada el esquema registra cuál era ese permiso en ese instante y escribe una línea en el log, lo que es un informe y no un veto: en una reproducción histórica el permiso nunca se concede, así que condicionar la entrada a él silenciaría todo el diagrama.

## Reglas de entrada y salida

- **Entrada en largo**: En una vela horaria terminada cuya hora del reloj coincide con Trading Hour, con la posición plana y el cierre de hace 29 barras por encima del cierre actual, comprar Volume a mercado bajo la condición Open position.
- **Entrada en corto**: En una vela horaria terminada cuya hora del reloj coincide con Trading Hour, con la posición plana y el cierre de hace 29 barras por debajo del cierre actual, vender Volume a mercado bajo la condición Open position.
- **Salida**: Position protection cierra la posición con un take-profit del 0.1% o un stop-loss del 0.5% medidos desde la ejecución de entrada, y el cierre de la vela alimenta sus comprobaciones de precio. Si no se alcanza ninguno de los dos niveles, el contador N values se dispara 21 velas terminadas después de la entrada y el bloque Close position liquida el resto; cuando la protección ya ha cerrado la posición, esa acción no encuentra nada que cerrar y no hace nada.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 01:00:00 | Marco temporal de las velas de trabajo. Solo se procesan velas terminadas, así que una orden nunca puede fecharse dentro de una barra que todavía se está formando. |
| Lookback Bars | 29 | Cuántas barras atrás se toma el cierre de referencia. Es la anchura de la ventana cuya deriva contrarresta la entrada. |
| Trading Hour | 7 | Hora del reloj a la que se abre la ventana de entrada diaria, leída del tiempo de la estrategia que acompaña a la vela terminada. |
| Volume | 0.1 | Cantidad fija de ambas órdenes de entrada. No hay dimensionamiento adaptativo: todas las entradas tienen el mismo tamaño. |
| Max Position Bars | 21 | Cuántas velas terminadas puede vivir una posición antes de ser cerrada con independencia de la ganancia o la pérdida. |
| Take Profit % | 0.1 | Movimiento favorable con el que Position protection cierra la posición, como porcentaje del precio de entrada. |
| Stop Loss % | 0.5 | Movimiento adverso con el que Position protection cierra la posición, como porcentaje del precio de entrada. El stop es fijo, no dinámico. |

## Detalles del diagrama

- El bloque [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) está configurado solo para velas terminadas, y [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) se aplica a la propia vela y no a un precio, con un [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) a continuación. Dos bloques [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) convierten los dos cierres en el lado largo y el corto; como ambas son estrictas, una ventana sin cambio no produce ninguno.
- [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) es aquí una fuente de datos, no una etiqueta: un convertidor lee su Hour y una comparación la contrasta con una [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html). Tanto la hora como la comprobación de posición plana están ancladas a la vela, porque las constantes con las que se comparan las dispara el flujo de velas, así que la puerta de entrada solo puede completarse una vez por barra terminada.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) y una comparación contra cero aportan la comprobación de posición plana, y los dos bloques [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) reúnen deriva, hora y posición en una sola señal por lado. Ambos bloques [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) llevan la condición Open position, que es la segunda salvaguarda frente a una entrada repetida mientras hay una posición abierta.
- La señal aceptada también arma [N values](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html), que cuenta velas terminadas y luego dispara un tercer bloque Position modify configurado como Close position. Ese bloque no lleva volumen: la cantidad a cerrar se deduce de la posición abierta, y una cuenta plana simplemente no genera ninguna orden.
- [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) une las ejecuciones de ambos lados de entrada para [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html). En paralelo, un [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) emite exactamente un pulso por entrada y lo reinicia el contador de antigüedad; ese pulso fija la lectura de [Is trade allowed](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) en una variable, que un bloque [String format](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) convierte en una línea de log de [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) por cada posición tomada.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
