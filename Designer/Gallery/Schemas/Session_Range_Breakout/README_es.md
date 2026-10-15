# Diagrama de la estrategia de ruptura del rango de sesión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama negocia rupturas de BTCUSDT sobre un rango móvil de ocho horas durante la sesión diurna UTC. Las velas horarias finalizadas definen los niveles y las decisiones, una única marca diaria admite el primer candidato direccional y las ramas nocturnas devuelven a cero la posición creada por el diagrama.

![schema](schema.svg)

## Resumen de la estrategia

- Cada vela horaria finalizada se desplaza un periodo antes de entrar en los indicadores Highest 8 y Lowest 8, configurados para emitir solo valores formados. En cada vela nueva, ambos niveles representan las ocho horas completas inmediatamente anteriores y siguen avanzando, en lugar de quedar fijos durante el día.
- Un reloj Time común activa el restablecimiento diario desde las 00:00:00 hasta las 07:59:59 UTC. La hora de apertura de la vela controla por separado la ventana de negociación de 08:00:00 a 19:59:59 y la de cierre de 20:00:00 a 23:59:59; estas configuraciones implementan los intervalos semiabiertos `[08:00, 20:00)` y `[20:00, 24:00)`.
- Dentro de la ventana de negociación, la condición estricta `Close > High` con `Position <= 0` forma el candidato largo, mientras que `Close < Low` con `Position >= 0` forma el candidato corto. La igualdad con cualquiera de los límites no activa una entrada.
- Ambas direcciones comparten un solo Flag, por lo que únicamente el primer candidato largo o corto apto puede entrar durante cada día UTC. La cantidad de entrada es `Base Volume + abs(Position)`: abre una unidad desde cero o cierra y revierte una posición contraria de una unidad mediante una sola orden de mercado.
- Durante la ventana de cierre, una posición positiva envía una venta a mercado por el volumen base y una posición negativa envía una compra a mercado por el mismo volumen. No hay bloques de stop-loss ni take-profit; el gráfico muestra las velas, los niveles móviles Highest y Lowest y los cuatro flujos MyTrade.

## Reglas de entrada y salida

- **Entrada en largo**: Entre las 08:00:00 y las 19:59:59 UTC, cuando una vela finalizada cumple `Close > Highest(8)` sobre las ocho horas anteriores, la instantánea de posición es `<= 0` y el Flag diario compartido está disponible, el diagrama envía una compra a mercado NoCondition por `1 + abs(Position)`.
- **Entrada en corto**: Entre las 08:00:00 y las 19:59:59 UTC, cuando una vela finalizada cumple `Close < Lowest(8)` sobre las ocho horas anteriores, la instantánea de posición es `>= 0` y el Flag diario compartido está disponible, el diagrama envía una venta a mercado NoCondition por `1 + abs(Position)`.
- **Salida**: Entre las 20:00:00 y las 23:59:59 UTC, el diagrama vende Base Volume 1 si la posición es positiva y compra Base Volume 1 si es negativa. En el funcionamiento normal, las entradas crean una exposición exactamente igual a `+1` o `-1`, por lo que la cantidad fija de salida la devuelve a cero. No hay stop-loss, take-profit ni otra protección conectada.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 01:00:00 | Marco temporal de una hora para BTCUSDT; solo las velas finalizadas alimentan el rango móvil, las comprobaciones horarias, las instantáneas de posición y las decisiones. |
| Range Length | 8 | Número de velas finalizadas y desplazadas que usan los indicadores Highest y Lowest con emisión solo de valores formados; la vela actual queda excluida. |
| Reset Window | 00:00:00–07:59:59 UTC | Intervalo UTC en el que el reloj Time común restablece el Flag diario compartido antes de la sesión de negociación. |
| Trade Window | 08:00:00–19:59:59 UTC | Límites UTC inclusivos configurados para candidatos de entrada, equivalentes al intervalo semiabierto `[08:00, 20:00)` según la hora de apertura de la vela horaria. |
| Close Window | 20:00:00–23:59:59 UTC | Límites UTC inclusivos configurados para cerrar posiciones, equivalentes al intervalo semiabierto `[20:00, 24:00)` según la hora de apertura de la vela horaria. |
| Base Volume | 1 | Cantidad unitaria usada al entrar desde cero, sumada a `abs(Position)` en las reversiones y entregada sin cambios a las dos órdenes nocturnas de salida. |

## Detalles del diagrama

- El bloque [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite velas horarias finalizadas de BTCUSDT. Un bloque [Valor anterior](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) con Shift 1 excluye la vela de decisión del cálculo del rango.
- Dos bloques de [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) configurados para emitir solo valores formados calculan Highest 8 y Lowest 8 con el flujo desplazado. Sus salidas se actualizan cada hora completa y describen el canal móvil de las ocho horas anteriores.
- Un flujo Time común controla el bloque de restablecimiento [Horario de trabajo](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) para 00:00:00–07:59:59 UTC. El flujo de velas controla directamente los bloques de horario de negociación y cierre, de modo que esas decisiones usan el OpenTime de cada vela.
- La [Posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/current.html) actual se muestrea en cada vela de decisión. Los bloques de [Comparación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) y [Condición lógica](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) combinan la ruptura estricta, la sesión, el lado de la posición y la marca compartida.
- La aritmética de volumen calcula `Base Volume + abs(Position)`. Los dos bloques de entrada [Modificar posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) colocan órdenes de mercado NoCondition: abren una unidad larga o corta desde cero, o revierten por completo el lado contrario de una unidad con una sola orden.
- La ventana de restablecimiento repone un Flag compartido para el día UTC y el primer candidato largo o corto aceptado lo consume. En la ventana de cierre, ramas separadas para posiciones positivas y negativas envían órdenes de mercado por Base Volume 1; así cierran la exposición `±1` producida por la ruta normal de entrada del diagrama.
- El [Panel del gráfico](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recibe velas finalizadas, Highest 8, Lowest 8 y las salidas MyTrade de la entrada larga, la entrada corta, la salida del largo y la salida del corto. No hay elementos de stop-loss ni take-profit.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
