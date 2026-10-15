# Cesta programada de dos patas con guarda monetaria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama opera un reloj, no una señal. No contiene ningún indicador: una vez al día se abre una breve ventana de entrada y el diagrama compra dos instrumentos en el mismo momento, una ventana posterior cierra ambas posiciones, y entre las dos ventanas una guarda monetaria vigila lo que ha ganado la cesta y la cierra antes de tiempo al alcanzar un objetivo de beneficio o un límite de pérdida.

![schema](schema.svg)

## Resumen de la estrategia

- Dos bloques Variable de tipo Security nombran los dos instrumentos que opera el diagrama. Cada uno alimenta su propia serie de velas, su propia acción de apertura y su propia acción de cierre, de modo que las dos patas se dimensionan y se gestionan por separado aunque se abran y se cierren juntas.
- Sync retiene las dos series de velas de 5 minutos hasta que ambas patas han entregado la misma barra y las libera como un único conjunto, de modo que el valor dibujado para la cesta nunca mezcla un precio nuevo de una pata con un precio obsoleto de la otra.
- Una Formula multiplica cada cierre liberado por el tamaño operado de esa pata y suma los dos productos. El resultado es lo que la cesta vale realmente con los tamaños que opera el diagrama, y es la línea dibujada en el panel del gráfico.
- Working time lee la marca de tiempo de las velas terminadas de la primera pata y solo está abierto dentro de la ventana de entrada, de modo que exactamente una barra al día lo atraviesa. Flag convierte esa apertura en un único pulso y permanece enclavado hasta que la cesta se cierra.
- El pulso enclavado dispara dos bloques Position modify configurados como Open position. Cada uno lleva su propio Security y su propio Volume, y la condición Open position hace que el par permanezca en silencio siempre que ese instrumento ya esté en cartera, de modo que un pulso nunca puede apilar una segunda cesta sobre la primera.
- Strategy P&L alimenta una Formula que suma el dinero realizado y el abierto. Una segunda Formula resta el importe capturado en el momento en que se abrió la cesta, lo que convierte el total acumulado de la cuenta en el resultado de la cesta actual por sí sola.
- Un OR lógico une tres motivos para cerrar: la ventana de cierre, un resultado de la cesta por encima del objetivo de beneficio y un resultado de la cesta por debajo del límite de pérdida. Su señal acciona dos bloques Position modify configurados como Close position y además libera el Flag diario, de modo que la siguiente ventana de entrada encuentra el enclavamiento libre.

## Reglas de entrada y salida

- **Entrada en largo**: Dentro de la ventana de entrada, la vela terminada de la primera pata abre Working time, el Flag diario todavía no se ha usado y ambos bloques Open position se disparan con el mismo pulso: uno compra la primera pata por su propio tamaño, el otro compra la segunda pata por el suyo. Cada orden es una orden a mercado, y cada una queda suprimida en su propio instrumento si ya hay una posición abierta en él.
- **Entrada en corto**: El diagrama no tiene lado corto: ambos bloques de entrada llevan una dirección Buy fija. Una cesta corta está a un solo ajuste de distancia: cambie el Direction de los dos bloques Open position a Sell y el mismo horario, el mismo enclavamiento y la misma guarda monetaria harán funcionar la cesta en sentido contrario.
- **Salida**: Tres motivos cierran la cesta, y basta con cualquiera de ellos. La ventana de cierre, accionada por el reloj de la estrategia y no por la llegada de velas, cierra según el horario; el resultado de la cesta subiendo por encima del objetivo de beneficio cierra antes de tiempo con ganancia; el resultado de la cesta cayendo por debajo del límite de pérdida cierra antes de tiempo con pérdida. Los tres pasan por un único OR lógico hacia dos bloques Close position, que no necesitan ni volumen ni dirección porque calculan ambos a partir de la posición que encuentran, y no hacen nada en absoluto cuando no hay ninguna, de modo que una señal repetida dentro de la ventana es inofensiva.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| First Leg Security | BTCUSDT@BNBFT | Instrumento operado por la primera pata; determina la serie de velas que marca el tiempo de la ventana de entrada. |
| Second Leg Security | TONUSDT@BNBFT | Instrumento operado por la segunda pata; se abre y se cierra con las mismas señales que el primero. |
| First Leg Candles | 00:05:00 | Marco temporal de la primera pata. Solo se usan velas terminadas, por lo que la ventana de entrada debe tener al menos el ancho de una vela. |
| Second Leg Candles | 00:05:00 | Marco temporal de la segunda pata. Manténgalo igual al de la primera pata, ya que ambas se retienen juntas antes de calcular el valor de la cesta. |
| Alignment Interval | 00:05:00 | Intervalo de agrupación usado para alinear las dos patas. Debería coincidir con el marco temporal de las velas; un valor mayor liberaría el par más tarde que la barra a la que pertenece. |
| Entry Window From | 10:00:00 | Comienzo de la ventana de entrada diaria, leído de la marca de tiempo de las velas terminadas de la primera pata. |
| Entry Window Until | 10:04:00 | Fin de la ventana de entrada diaria. El intervalo entre los dos valores debe contener exactamente una apertura de vela; de lo contrario, el enclavamiento diario se armaría más de una vez. |
| Flatten Window From | 17:00:00 | Comienzo de la ventana de cierre diaria, leído del reloj de la estrategia y no de la llegada de velas. |
| Flatten Window Until | 17:10:00 | Fin de la ventana de cierre diaria. Manténgala con un ancho de unas pocas velas para que el reloj se muestree dentro de ella al menos una vez. |
| First Leg Size | 0.01 | Cantidad usada para abrir la primera pata, y peso que la primera pata aporta al valor de la cesta dibujado. |
| Second Leg Size | 100 | Cantidad usada para abrir la segunda pata, y peso que la segunda pata aporta al valor de la cesta dibujado. Elíjala de modo que ambas patas aporten importes de dinero comparables. |
| Profit Target | 100 | Dinero ganado por la cesta actual por encima del cual se cierra antes de tiempo. Se mide desde el momento en que se abrió la cesta, no desde el inicio de la ejecución. |
| Loss Limit | -500 | Dinero perdido por la cesta actual por debajo del cual se cierra antes de tiempo; un valor negativo, medido de la misma manera que el objetivo de beneficio. |

## Detalles del diagrama

- Dos bloques [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) de tipo Security son el único lugar donde se nombra un instrumento. Cada uno alimenta un bloque [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) y la entrada Security de los dos bloques Position modify que gestionan esa pata, de modo que una pata se reapunta a otro instrumento editando un solo valor.
- [Sync](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/sync.html) toma una línea por pata y deja salir ambas juntas una vez que la barra está completa en las dos. Las velas liberadas se dibujan en el panel del gráfico y se convierten en precios de cierre, que una [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) pondera por los tamaños operados hasta formar la línea del valor de la cesta.
- Los dos relojes son deliberadamente distintos. El [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) de entrada se alimenta de las velas de la primera pata, de modo que la decisión de entrada no puede desviarse del precio al que se toma; el Working time de cierre se alimenta de [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html), de modo que el cierre se produce igualmente cuando los datos se quedan en silencio. [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) se sitúa entre la ventana de entrada y las órdenes y solo se reinicia con la señal de cierre, que es lo que limita el diagrama a una cesta por día.
- [P&L change](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) informa del dinero realizado y del abierto en cada actualización. Sumarlos da el total acumulado de la cuenta; una Variable captura ese total en la primera ejecución de entrada, una segunda Variable lo conserva y lo vuelve a emitir en cada actualización posterior, y restarlo deja el resultado de la cesta que está abierta ahora. La guarda mide por tanto la cesta actual y no la vida entera de la cuenta, y vuelve a cero en cuanto la cesta se cierra.
- Cada acción es un bloque [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html). Los dos bloques de entrada usan la condición Open position con dirección y volumen explícitos; los dos bloques de salida usan Close position, que no toma ninguno de los dos y deriva ambos de la posición actual de su propio instrumento. [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) fusiona los cuatro flujos de ejecuciones en la única serie de operaciones dibujada en el panel del gráfico, mientras que los cuatro flujos de órdenes se dibujan por separado.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
