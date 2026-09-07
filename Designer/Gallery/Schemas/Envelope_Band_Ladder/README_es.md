# Diagrama de la estrategia Envelope Band Ladder
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama opera reversión a la media con bandas de Bollinger en velas de cinco minutos, mediante una escalera de entrada de dos tramos, reversiones acotadas, salidas en la banda media y cancelación programada de órdenes pendientes.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas finalizadas de cinco minutos alimentan Bollinger Bands con periodo 20 y ancho 1.5. Las comparaciones estrictas detectan un cierre bajo la banda inferior o sobre la superior únicamente cuando el indicador está formado.
- Las entradas se aceptan de 00:00:00 a 16:59:59 UTC, ambos extremos incluidos. Desde posición plana, el primer tramo es una orden de mercado de una unidad y el segundo es un límite pendiente de una unidad en `2 × lower − middle` para compra o `2 × upper − middle` para venta.
- Una señal contraria a una posición de uno o dos tramos cancela el límite anterior y envía una orden de mercado de tres unidades. Así, cualquier exposición permitida pasa a una o dos unidades en la nueva dirección sin añadir otro tramo lejano.
- El retorno a través de la banda media tiene menos prioridad que una entrada simultánea más allá de la banda opuesta. La salida cancela el tramo pendiente y encadena dos acciones ReduceOnly de mercado de una unidad; la segunda actúa solo si queda otra unidad.
- Fuera de la ventana de entrada, un Flag diario activa la cancelación masiva y la dirigida. El horario no cierra la exposición abierta; las salidas por la banda media siguen activas todo el día.

## Reglas de entrada y salida

- **Entrada en largo**: Durante la ventana UTC, `Close < lower band` y `Position ≤ 0` forman un candidato de compra. Desde posición plana se envían una compra de mercado de una unidad y un límite lejano de una unidad en `2 × lower − middle`; desde corto se cancelan límites antiguos y se compran tres unidades para revertir la posición acotada.
- **Entrada en corto**: Durante la ventana UTC, `Close > upper band` y `Position ≥ 0` forman un candidato de venta. Desde posición plana se envían una venta de mercado de una unidad y un límite superior de una unidad en `2 × upper − middle`; desde largo se cancelan límites antiguos y se venden tres unidades para revertir la posición acotada.
- **Salida**: Si no existe una entrada contraria de mayor prioridad, el largo sale tras `Close > middle band` y el corto tras `Close < middle band`. Primero se cancelan los tramos pendientes; dos acciones ReduceOnly de mercado, encadenadas y de una unidad, eliminan hasta dos tramos ejecutados sin cruzar la posición plana. El filtro horario cancela órdenes, pero no fuerza el cierre.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de las velas finalizadas empleado por todos los cálculos de señal. |
| Bollinger Length | 20 | Periodo retrospectivo del indicador Bollinger Bands ya formado. |
| Bollinger Width | 1.5 | Número de desviaciones estándar usado para las bandas superior e inferior. |
| Entry Start | 00:00:00 UTC | Inicio UTC inclusivo de la ventana fija de entrada. |
| Entry End | 16:59:59 UTC | Fin UTC inclusivo de la ventana fija de entrada; después se cancelan los límites pendientes. |
| Rung Volume | 1 | Cantidad de cada tramo normal de la escalera y de cada paso de salida ReduceOnly. |

## Detalles del diagrama

- El bloque [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite velas finalizadas de cinco minutos y puede construirlas desde el historial de un minuto incluido. Un bloque [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) calcula las tres líneas de Bollinger.
- Los bloques [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html), [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) y [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) extraen Close y aplican las puertas estrictas de banda, posición, sesión y prioridad.
- El bloque [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) es un filtro fijo del diagrama. Los bloques Formula y Variable calculan y fijan los precios lejanos y la cantidad triple de reversión en el instante de entrada.
- Seis bloques [Order registering](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) cubren entradas de mercado desde plano, reversiones de mercado acotadas y los dos límites pendientes. Los bloques [Mass order cancellation](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html) marcan cada frontera de cancelación, y dos cancelaciones dirigidas conservan y cancelan los límites lejanos activos.
- Dos bloques [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) ejecutan la salida ReduceOnly encadenada. El [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) muestra velas, las tres bandas y el flujo de operaciones de la estrategia.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
