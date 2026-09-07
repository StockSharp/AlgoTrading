# Diagrama de la estrategia Lunch Break Fade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama opera el retroceso del movimiento de corto plazo durante la ventana de almuerzo de 11:00:00 a 14:59:59 con velas finalizadas de cinco minutos. Entra solo desde una posición plana, sale comparando el cierre con una SMA formada de 20 períodos y bloquea todas las rutas de entrada y salida durante las siguientes 30 velas completadas después de una señal de orden.

![schema](schema.svg)

## Resumen de la estrategia

- Solo las velas finalizadas de cinco minutos entran en la cadena de decisión. La SMA comienza a emitir después de su calentamiento de 20 períodos y dos bloques Previous value proporcionan los dos cierres inmediatamente anteriores.
- El bloque Working time lee la hora de apertura de cada vela y permite entradas desde las 11:00:00 hasta las 14:59:59, incluidos ambos límites. La ventana horaria no restringe las salidas.
- Dos cierres anteriores ascendentes seguidos de una vela bajista generan una entrada corta desde una posición plana. Dos cierres descendentes seguidos de una vela alcista generan una entrada larga desde una posición plana.
- Una posición larga sale cuando el cierre está por debajo de la SMA y una posición corta sale cuando el cierre está por encima de la SMA. Cuatro rutas independientes envían órdenes a mercado de volumen fijo para las dos entradas y las dos salidas.
- Cada señal de entrada o salida activa una pausa que bloquea ambos tipos de acción durante las siguientes 30 velas completadas. No hay un bloque de protección de posición; el gráfico muestra las velas, la SMA y las ejecuciones de las cuatro rutas de órdenes.

## Reglas de entrada y salida

- **Entrada en largo**: Durante la ventana de almuerzo, si `Close[-1] < Close[-2]`, la vela actual es alcista (`Close > Open`), la instantánea de posición es cero y la pausa ha terminado, el diagrama envía una compra a mercado con Volume 1.
- **Entrada en corto**: Durante la ventana de almuerzo, si `Close[-1] > Close[-2]`, la vela actual es bajista (`Close < Open`), la instantánea de posición es cero y la pausa ha terminado, el diagrama envía una venta a mercado con Volume 1.
- **Salida**: Con la pausa terminada, una posición larga envía una venta a mercado cuando `Close < SMA` y una posición corta envía una compra a mercado cuando `Close > SMA`. Estas comprobaciones de nivel se ejecutan tanto dentro como fuera de la ventana de almuerzo. No se conecta stop-loss, take-profit ni ninguna otra protección.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de cinco minutos; solo las velas finalizadas activan el indicador, el historial, la pausa y las decisiones. |
| SMA Period | 20 | Período de la SimpleMovingAverage utilizado por ambas comprobaciones de nivel de salida. |
| Cooldown Bars | 30 | Cantidad de velas completadas posteriores durante las cuales se bloquean las señales de entrada y salida. |
| Lunch Begin | 11:00:00 | Límite inclusivo de la hora de apertura a partir del cual se habilitan las entradas del almuerzo. |
| Lunch End | 14:59:59 | Límite inclusivo de la hora de apertura hasta el cual permanecen habilitadas las entradas del almuerzo. |
| Volume | 1 | Cantidad fija suministrada a los cuatro bloques de órdenes a mercado con NoCondition. |

## Detalles del diagrama

- El bloque [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite velas finalizadas de cinco minutos. Un [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) limitado a valores formados calcula SimpleMovingAverage 20 y una [Fórmula](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/formula.html) expone su valor numérico. La puerta [Operación permitida](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) libera la vela almacenada en la cadena de decisión.
- Los bloques [Convertidor](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) extraen los precios Close y Open. Dos bloques [Valor anterior](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) usan desplazamientos 1 y 2; una puerta de historial preparado impide las decisiones hasta que estén disponibles ambos cierres anteriores.
- El bloque [Horario de trabajo](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) recibe directamente el flujo de velas y compara los metadatos de la hora de apertura con los límites inclusivos de la ventana de almuerzo. Su resultado participa solo en las dos condiciones de entrada.
- La [Posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/current.html) actual se guarda en una [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) y se emite una vez por cada vela de decisión. Los bloques de [Comparación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) y [Condición lógica](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) combinan sesión, dirección anterior, dirección de la vela, posición, historial preparado, nivel de SMA y estado de la pausa.
- Un bloque [Retrasar señal](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) cuenta 30 velas finalizadas posteriores. Las variables de estado bloquean las cuatro condiciones de acción durante el recuento y vuelven a habilitarlas en la vela siguiente.
- Cuatro bloques [Modificar posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) colocan órdenes a mercado con `NoCondition` y Volume 1 compartido: compra de entrada, venta de entrada, venta de salida del largo y compra de salida del corto. No hay elemento de protección.
- El [Panel de gráfico](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recibe velas finalizadas, el flujo de SMA formado y la salida MyTrade de cada uno de los cuatro bloques de órdenes.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
