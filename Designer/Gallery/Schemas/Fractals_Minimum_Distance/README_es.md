# Diagrama de la estrategia de distancia mínima entre fractales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un fractal es una vela que queda más alta, o más baja, que las dos velas situadas a cada lado, y solo puede darse por confirmado una vez que esas velas posteriores existen. Este diagrama localiza ambos tipos, recuerda el precio del más reciente de cada color y se niega a operar hasta que los dos estén lo bastante separados como para que merezca la pena operar entre ellos. Todo se mide sobre velas terminadas, de modo que un nivel queda fijado en el momento en que se declara y nunca se revisa después.

![schema](schema.svg)

## Resumen de la estrategia

- Una sola serie de velas alimenta todo el diagrama y entrega únicamente velas terminadas, así que ningún nivel, ninguna distancia y ninguna orden se construye jamás a partir de un precio que un tick posterior aún podría retirar.
- Previous value devuelve la vela de una barra atrás, y Highest y Lowest miden el extremo de las cinco velas que terminan ahí: una ventana que queda íntegramente en el pasado.
- Un segundo Previous value devuelve la vela de tres barras atrás, el centro exacto de esa ventana, y dos convertidores leen su máximo y su mínimo.
- Cuando el máximo de la vela central coincide con el valor más alto de la ventana, el diagrama tiene un fractal superior, y la prueba especular sobre el mínimo marca uno inferior. Una variable con retención guarda el precio de cada fractal y, como la retención ignora una comparación falsa, el precio guardado solo cambia en una barra que realmente haya impreso un fractal.
- Un segundo par de variables vuelve a emitir los dos niveles almacenados en cada vela, de modo que la fórmula que mide la separación entre ellos y la comparación contra la distancia mínima producen ambas una respuesta en cada barra, y no solo en las barras con fractal.
- Cada lado combina su propia prueba de fractal con esa prueba de distancia en una condición lógica; la señal aceptada primero cierra la posición opuesta y solo después abre una nueva, ambas a mercado.
- Position protection sigue a cada ejecución y calcula el precio de su salida a partir del cierre de cada vela terminada, así que el take-profit o el stop-loss se comprueba una vez por barra.
- Las velas, ambos extremos, ambos niveles almacenados y cada ejecución se dibujan en una misma área del gráfico, de modo que los dos niveles y la separación entre ellos pueden leerse directamente de la imagen.

## Reglas de entrada y salida

- **Entrada en largo**: Se confirma un fractal inferior —el mínimo de tres barras atrás es el más bajo de la ventana de cinco velas que termina una barra atrás— y la distancia entre el último nivel superior y el último nivel inferior es al menos la distancia mínima. El diagrama cierra la posición corta si hay alguna abierta y a continuación compra el volumen de la orden a mercado.
- **Entrada en corto**: Se confirma un fractal superior —el máximo de tres barras atrás es el más alto de esa misma ventana— bajo la misma condición de distancia. El diagrama cierra la posición larga si hay alguna abierta y a continuación vende el volumen de la orden a mercado.
- **Salida**: Dos cosas pueden terminar una operación. Un fractal del color opuesto cierra lo que esté abierto antes de enviar la nueva entrada, y por eso en cada señal el bloque de cierre va delante del de apertura. Al margen de eso, Position protection cierra la posición en un take-profit o un stop-loss medidos en porcentaje del precio de ejecución y comprobados contra el cierre de cada vela terminada.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de la única serie de velas. Solo se entregan velas terminadas, así que se toma una decisión por barra. |
| Upper Fractal Length | 5 | Número de velas de la ventana con cuyo máximo más alto se compara la vela central. |
| Lower Fractal Length | 5 | Número de velas de la ventana con cuyo mínimo más bajo se compara la vela central; mantenlo igual al superior para que el fractal siga siendo simétrico. |
| Fractal Shift | 3 | Cuántas barras atrás se sitúa la vela central. Con una ventana de cinco retrasada una barra, tres la coloca exactamente en su centro. |
| Minimum Distance | 100 | Separación mínima entre el último nivel superior y el último nivel inferior que todavía permite una entrada. Es una distancia absoluta en las unidades de precio del instrumento, por lo que hay que reescalarla cada vez que el diagrama se traslade a un instrumento cotizado en un orden de magnitud distinto. |
| Order Volume | 1 | Tamaño de la orden, en lotes. |
| Take Profit, % | 2 | Distancia del take-profit, en porcentaje del precio de ejecución, comprobada en el cierre de cada vela terminada. |
| Stop Loss, % | 1 | Distancia del stop-loss, en porcentaje del precio de ejecución, comprobada en el cierre de cada vela terminada. |

## Detalles del diagrama

- El bloque del indicador marca como valor completado todo lo que le llega: no tiene forma de distinguir una vela en formación de una cerrada. Entregar únicamente velas terminadas es lo que mantiene fuera de la ventana los máximos y mínimos a medio formar, que allí desplazarían en silencio el extremo para luego ser sobrescritos en la siguiente actualización.
- Esa misma suscripción es la que mantiene válidas las órdenes: una orden construida a partir de la actualización de una vela sin terminar lleva la hora de apertura de esa barra y se rechaza por llegar del pasado.
- La ventana se retrasa una barra para que la vela que se está juzgando quede exactamente en su centro, con dos velas antes y dos después. Por eso un fractal nunca se declara antes de tres barras después de haber ocurrido, y nunca se revisa más tarde.
- El disparador de retención descarta una comparación falsa en lugar de almacenar un valor, y eso es lo que convierte «la prueba se cumplió en esta barra» en «este es el último precio al que se cumplió». Ninguno de los dos niveles existe hasta su primer fractal, así que no es posible ninguna entrada antes de haber visto ambos colores.
- Ambos bloques de entrada operan a mercado y tienen indicado que no exigen que la estrategia esté online, de modo que el diagrama se comporta igual sobre historial grabado que en negociación en vivo.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
