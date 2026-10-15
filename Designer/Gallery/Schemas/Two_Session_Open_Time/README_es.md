# Diagrama de la estrategia de apertura en dos sesiones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama no contiene ningún indicador: el reloj es la única fuente de señales. Dos ventanas independientes de la jornada de negociación abren cada una una posición larga, un bloque Flag limita cada ventana a una única entrada por día y una tercera ventana liquida lo que siga abierto y vuelve a armar ambas ventanas para el día siguiente.

![schema](schema.svg)

## Resumen de la estrategia

- Time transmite el momento actual a tres bloques Working time: dos ventanas de entrada, de 09:30 a 14:00 y de 00:00 a 04:00, y una ventana de cierre forzoso, de 19:50 a 20:00.
- Cada ventana de entrada se combina con la comprobación de posición plana mediante un bloque Logical condition configurado como And, de modo que una ventana solo puede pedir una entrada mientras no haya nada abierto.
- Una ventana permanece abierta durante horas y su puerta repite una y otra vez el mismo valor verdadero. Flag se sitúa entre la puerta y la orden y deja pasar solo el primero de ellos, lo que convierte una ventana larga en una única entrada.
- Ambas ventanas compran. Position modify actúa con la condición Open position, por lo que una orden de mercado por Order Volume solo sale cuando la posición es exactamente cero.
- La ventana de cierre forzoso acciona un tercer Position modify configurado como Close position, y esa misma señal reinicia ambos bloques Flag, de modo que las dos ventanas de entrada quedan armadas de nuevo para el día siguiente.
- Position protection vigila las ejecuciones de ambas entradas y cierra la posición con un take-profit del 1.5% o un trailing stop del 0.5% que sigue al cierre de la vela.
- Las velas de cinco minutos terminadas marcan el ritmo de todo el diagrama: llevan el precio de cierre a Position protection, son lo que dibuja el panel y su llegada es lo que hace avanzar el reloj.
- El panel del gráfico muestra las velas, la línea de precio a la que reacciona la protección, todas las órdenes que envía el diagrama y todas las ejecuciones que recibe.

## Reglas de entrada y salida

- **Entrada en largo**: Dentro de cualquiera de las dos ventanas, mientras la posición está plana, el Flag de esa ventana libera su primera señal verdadera y Position modify compra Order Volume a mercado bajo la condición Open position. Cualquier señal posterior de la misma ventana queda absorbida por el Flag hasta que la ventana de cierre lo reinicia.
- **Entrada en corto**: No hay lado corto. Ambas ventanas abren en largo, y las únicas órdenes de venta que llega a enviar el diagrama son las que cierran una posición larga abierta.
- **Salida**: Position protection cierra la posición con un take-profit del 1.5% o un trailing stop del 0.5% que sigue al cierre de la vela. Todo lo que siga abierto cuando comienza la ventana de cierre se liquida mediante la acción Close position, que además borra ambos cerrojos.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de cinco minutos; solo se procesan las velas terminadas, y sus cierres son la base del precio que comprueba la protección y de la línea del gráfico. |
| First Window From | 09:30:00 | Inicio de la primera ventana de entrada en hora de reproducción o del servidor. |
| First Window Until | 14:00:00 | Fin de la primera ventana de entrada; a partir de ahí esa ventana ya no puede armar una entrada. |
| Second Window From | 00:00:00 | Inicio de la segunda ventana de entrada en hora de reproducción o del servidor. |
| Second Window Until | 04:00:00 | Fin de la segunda ventana de entrada. |
| Close Window From | 19:50:00 | Inicio de la ventana de cierre forzoso, que liquida una posición abierta y reinicia ambos cerrojos. |
| Close Window Until | 20:00:00 | Fin de la ventana de cierre forzoso. |
| Order Volume | 1 | Cantidad fija que utilizan las entradas de ambas ventanas. |
| Take Profit, % | 1.5 | Movimiento porcentual favorable con el que Position protection cierra la posición. |
| Stop Loss, % | 0.5 | Movimiento porcentual adverso del stop; la técnica de trailing lo desplaza por detrás del cierre de la vela una vez que el precio avanza a favor. |

## Detalles del diagrama

- El bloque [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite velas de cinco minutos terminadas. Un [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) toma su precio de cierre, que es el precio contra el que [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) mide su take-profit y su trailing stop, y también la línea que se dibuja junto a las velas. No se calcula nada más a partir del precio: el diagrama no incluye ningún indicador.
- [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) suministra el momento actual a tres bloques [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html). Dos de ellos delimitan las ventanas de entrada y uno la ventana de cierre forzoso; la reproducción del historial incluido se ejecuta en UTC, por lo que los límites de las ventanas se leen como horas UTC.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) comparado con una [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) en cero a través de [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) produce la comprobación de posición plana que comparten las dos puertas And de [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html), de modo que una posición abierta bloquea en silencio también la otra ventana.
- [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) es lo que convierte una ventana en un único evento. Su disparador es la puerta And y su reinicio es la ventana de cierre; solo deja pasar un valor en el instante en que se activa, por lo que los cientos de lecturas verdaderas que produce una ventana de cuatro horas se reducen a una sola entrada.
- Actúan tres bloques [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html): dos entradas Open position que toman Order Volume y un cierre Close position que no necesita volumen porque lee la posición que debe deshacer. Las ejecuciones de ambas entradas se unen mediante [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) y se entregan a Position protection, cuya propia ejecución de cierre se dibuja en el panel pero no se realimenta a su entrada de operaciones.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
