# Diagrama de la estrategia Fractal Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un fractal es una vela cuyo máximo es el más alto de cinco, o cuyo mínimo es el más bajo de cinco, y solo puede identificarse dos velas después de haber ocurrido. El diagrama respeta ese retardo con honestidad: trabaja únicamente con velas completadas, convierte cada fractal en un precio de stop y deja que ese precio se mueva en una sola dirección. Cada ruptura de un stop invierte la posición, y el nivel roto queda apartado hasta que un nuevo fractal vuelve a activarlo.

![schema](schema.svg)

## Resumen de la estrategia

- Una única serie de velas alimenta todo el diagrama y está configurada para entregar solo velas completadas, de modo que ningún nivel y ninguna orden se construyen sobre un precio que un tick posterior todavía podría revertir.
- Highest y Lowest toman el extremo de las últimas cinco velas, mientras que Previous value devuelve la vela de dos barras atrás y dos conversores leen su máximo y su mínimo.
- Cuando el máximo de dos barras atrás es el más alto de la ventana, esa vela es un fractal superior; la prueba simétrica sobre el mínimo marca un fractal inferior. Una variable de retención guarda el precio de cada fractal y solo lo deja salir en la barra en la que la prueba se cumplió.
- Una fórmula suma el porcentaje de holgura al precio del fractal superior y se lo resta al inferior, convirtiendo un fractal en un precio de stop.
- Otras dos fórmulas aplican un trinquete a esos precios con min y max: el stop superior solo puede bajar y el stop inferior solo puede subir, que es lo que los convierte en trailing stops y no en simples niveles de swing.
- El cierre de cada vela completada se mide contra ambos stops: por encima del stop superior el diagrama se invierte a largo, por debajo del stop inferior se invierte a corto.
- Una inversión es un par de bloques, uno que cierra lo que está abierto y otro que abre el nuevo lado, y el stop que se acaba de usar queda aparcado fuera de alcance mientras siga viva la posición que abrió.
- Las velas, ambos niveles de stop y cada ejecución se dibujan en una misma área del gráfico, de modo que el trinquete puede leerse directamente en la imagen.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre de una vela completada sube por encima del trailing stop superior. Position modify en modo Close recompra el corto si hay uno abierto, y Position modify en modo Open compra el volumen de la orden en cuanto la cuenta queda sin posición.
- **Entrada en corto**: El cierre de una vela completada cae por debajo del trailing stop inferior mientras el precio sigue por debajo del superior. El mismo par funciona al revés: primero se cierra el largo y después se vende el volumen de la orden.
- **Salida**: No hay una regla de salida separada. Una posición se mantiene hasta que se rompe el stop contrario, y esa ruptura la cierra y abre el lado inverso a la vez, así que el diagrama siempre está largo, corto o a una ejecución de estarlo. El trailing stop se ajusta a medida que aparecen nuevos fractales, y eso es lo que arrastra el precio de salida por detrás de una operación abierta.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de la única serie de velas. Solo se entregan velas completadas, así que se toma una decisión por barra. |
| Upper Fractal Length | 5 | Número de velas sobre las que se mide el extremo superior. La vela examinada se sitúa en el centro de esa ventana. |
| Lower Fractal Length | 5 | La misma ventana para el extremo inferior; mantenla igual que la superior, o los dos lados leerán fractales de anchuras distintas. |
| Fractal Shift | 2 | Cuántas barras atrás se sitúa la vela examinada. Tiene que ser el centro de la ventana: dos para una ventana de cinco, tres para una ventana de siete. |
| Stop Buffer, % | 0 | Porcentaje que se suma al precio del fractal superior y se resta al inferior antes de usar el nivel. Cero coloca el stop justo sobre el fractal; un valor mayor lo mantiene algo más alejado del precio. |
| Order Volume | 1 | Tamaño de la orden, en lotes. Se usa el mismo tamaño para ambos lados, así que una inversión es un cierre del tamaño anterior seguido de una apertura de este. |

## Detalles del diagrama

- Las velas completadas son lo que impide que los niveles se repinten. El bloque del indicador trata como definitivo cada valor que recibe, así que una vela aún en formación se escribiría en Highest y Lowest y después se sobrescribiría en su siguiente actualización; entregar solo velas completadas significa que los dos indicadores nunca ven un valor que pueda cambiar.
- La prueba del fractal se lee como «igual o superior» y no como «superior», de modo que un techo plano en el que dos velas comparten el mismo máximo sigue contando como fractal, y lo mismo ocurre con un doble suelo en el lado del mínimo.
- Un fractal se identifica dos velas tarde por construcción, y el diagrama no intenta ocultarlo: el nivel que produce es el que era cierto dos barras atrás, y se usa desde la barra en la que pasa a conocerse.
- Mientras hay una posición abierta, el stop que la abrió queda aparcado muy fuera de alcance y se vuelve a armar con el primer fractal que se forme una vez cerrada la posición. Sin eso, el diagrama seguiría señalando el lado en el que ya está, y el trinquete arrastraría el nivel tan lejos del precio que nunca podría volver a cruzarse.
- Si alguna vez ambos stops se leyeran como rotos en el mismo instante, el lado largo tiene prioridad: la rama corta lleva la condición adicional de que el precio siga por debajo del stop superior, así que las dos nunca pueden dispararse a la vez.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
