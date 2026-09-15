# Diagrama de la estrategia de debilidad del lunes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama opera con un calendario semanal fijo. Cada lado de la semana tiene su propio día: en el día corto vende cuando el cierre se sitúa por debajo de la SMA 20, y en el día de recompra vuelve a comprar esa posición corta; en el día largo compra cuando el cierre se sitúa por encima de la SMA 20, y en el día de salida vende esa posición larga. El día de la semana se lee directamente de la vela como un número, de modo que las cuatro reglas de calendario son cuatro comparaciones corrientes, y una ventana de entrada gobernada por el reloj mantiene la decisión semanal dentro de la parte activa de la jornada.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cinco minutos se entregan únicamente ya cerradas. Dos bloques Converter leen la misma vela: uno toma el precio de cierre y el otro toma el día de la semana de la hora de apertura, que llega como un número en el que el domingo es 0 y el sábado es 6.
- Cuatro bloques Variable guardan los cuatro días del calendario -corto, recompra, largo y salida- y cuatro bloques Comparison ajustados a Equal convierten el número de día en cuatro señales. Exactamente uno de ellos puede ser verdadero en una vela dada, y eso es lo que impide que las cuatro ramas lleguen a competir entre sí.
- La SMA 20 se calcula sobre las mismas velas y no publica nada hasta estar formada, por lo que las veinte primeras velas de la ejecución no producen ninguna señal. Dos bloques Comparison la leen: uno es verdadero mientras el cierre está por debajo de la media y el otro mientras el cierre está por encima de ella.
- Un bloque Position, una Variable que guarda cero y una Comparison ajustada a Equal proporcionan la comprobación de posición plana. Ambas ramas de entrada la exigen, de modo que una semana que ya está en el mercado no puede acumular una segunda posición encima.
- Current time alimenta a Working time, que es verdadero entre las 08:00:00 y las 20:00:00. Ambas ramas de entrada exigen también esa ventana, así que nunca se abre una posición semanal en una vela nocturna de poca actividad. Las dos salidas quedan deliberadamente fuera de la ventana: lo que esté abierto debe cerrarse en su día de calendario, a la hora a la que aparezca la señal.
- Dos bloques Logical condition AND reúnen las entradas. La rama corta necesita el día corto, un cierre por debajo de la SMA 20, una posición plana y la ventana abierta; la rama larga necesita el día largo, un cierre por encima de la SMA 20 y esos mismos dos filtros. Cada una acciona un bloque Modify position con la condición Open position, de modo que una señal repetida dentro del mismo día no puede enviar una segunda orden.
- Las dos salidas son bloques Modify position con la condición Close position y un lado explícito. El bloque del día de recompra es una compra, así que solo puede cerrar una posición corta; el bloque del día de salida es una venta, así que solo puede cerrar una posición larga. Ninguno lleva entrada de volumen, porque Close position dimensiona la orden a partir de la propia posición abierta.
- El Chart panel dibuja la serie de velas, la SMA 20, las órdenes de las cuatro acciones y las ejecuciones que producen, de modo que el ritmo semanal -entrada a principios de semana, recompra a mitad de semana, entrada a finales de semana y salida al final- puede leerse directamente en la imagen.

## Reglas de entrada y salida

- **Entrada en largo**: En el día largo, dentro de la ventana de entrada, con la posición plana y el cierre por encima de la SMA 20, se envía una compra a mercado de Order volume a través de Modify position con la condición Open position.
- **Entrada en corto**: En el día corto, dentro de la ventana de entrada, con la posición plana y el cierre por debajo de la SMA 20, se envía una venta a mercado de Order volume a través de Modify position con la condición Open position.
- **Salida**: Las salidas son por calendario, no por precio. En el día de recompra, una compra de Modify position con la condición Close position cierra una posición corta abierta; en el día de salida, una venta de Modify position con la misma condición cierra una posición larga abierta. No hay stop, ni objetivo, ni regla de trailing, por lo que una posición se mantiene hasta que llega su propio día de salida. Las dos señales de salida se repiten en cada vela de su día y nada cuenta ni suprime esa repetición: la primera vela cierra la posición y, a partir de ahí, Close position no tiene con qué trabajar y rechaza la señal en silencio. Lo mismo vale para las entradas: no hay contador por día ni pausa entre operaciones, y es la condición Open position junto con la comprobación de posición plana lo que limita cada día de calendario a una sola orden.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de la serie de velas. Solo se entregan velas cerradas; el día de la semana, la media y todas las señales se leen de velas completadas. |
| MA Period | 20 | Período de la media móvil simple que filtra ambas entradas. La media solo publica valores una vez formada, por lo que no es posible ninguna entrada durante las primeras velas de la ejecución. |
| Session From | 08:00:00 | Inicio de la ventana diaria en la que se permiten las entradas, leído del reloj de la estrategia. Las salidas ignoran esta ventana. |
| Session Until | 20:00:00 | Fin de esa ventana. Amplíe el par para que la regla de calendario pueda actuar a cualquier hora; estréchelo para concentrar las entradas en unas pocas horas del día. |
| Short day | 1 | Número de día que abre una posición corta cuando el cierre está por debajo de la media. Los días se numeran desde el domingo como 0 hasta el sábado como 6. |
| Cover day | 3 | Número de día en el que se recompra una posición corta abierta. Solo cierra cortos; ese día una posición larga queda intacta. |
| Long day | 4 | Número de día que abre una posición larga cuando el cierre está por encima de la media. Numerado en la misma escala con el domingo como 0. |
| Exit day | 5 | Número de día en el que se vende una posición larga abierta. Solo cierra largos; ese día una posición corta queda intacta. |
| Order volume | 1 | Cantidad fija que utilizan ambas acciones Open position. Las dos acciones Close position no necesitan volumen, porque dimensionan su orden a partir de lo que esté abierto. |

## Detalles del diagrama

- El bloque [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) está configurado para entregar solo velas cerradas, lo que importa por partida doble: el día de la semana se toma de una vela que ya no va a cambiar, y cada orden que envía el diagrama queda sellada con la hora de cierre de una vela completada y no con la hora de apertura de otra todavía en formación.
- Tanto el calendario como el precio proceden de un par de bloques [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) sobre esa serie. Leer el día de la semana de la propia vela y no de un reloj aparte mantiene la prueba de calendario exactamente al mismo compás que la prueba de tendencia, de modo que ambas describen siempre la misma vela cuando los bloques AND de [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) las reúnen.
- Los cuatro números de día son [Variables](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) corrientes comparadas con bloques [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html), y por eso todo el plan semanal se puede reorganizar desde la lista de parámetros: basta con mover el día corto a otro número para que el esquema opere ese día, sin tocar ningún enlace.
- [Current time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) y [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) aportan el filtro de hora del día como un nivel simple: verdadero durante toda la ventana y falso fuera de ella. Está conectado únicamente a las dos ramas de entrada, y la instantánea de [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) que está junto a él aporta de la misma manera la comprobación de posición plana.
- Cuatro bloques [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) se encargan de toda la operativa: dos con Open position y un volumen fijo, y dos con Close position y un lado declarado. Dar un lado a los bloques de cierre es lo que hace exactas las salidas por calendario: una compra del día de recompra sencillamente rechaza una posición larga, y una venta del día de salida sencillamente rechaza una corta. Todo lo que emiten se dibuja en el [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) junto con las velas y la SMA 20.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
