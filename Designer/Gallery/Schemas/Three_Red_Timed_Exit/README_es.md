# Diagrama de estrategia de tres velas rojas con salida temporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama opera tres velas consecutivas del mismo color cuando la volatilidad es elevada. Tres velas rojas con ATR 14 por encima de 0,8 veces su media de 30 valores generan una configuración larga; tres velas verdes con el mismo filtro generan una configuración corta. Desde una posición plana se entra directamente, una posición contraria se invierte mediante dos pasos confirmados por ejecución y una posición abierta puede cerrarse por el patrón opuesto o tras veinte barras finalizadas. Cada secuencia de acciones inicia una pausa de doce velas.

![schema](schema.svg)

## Resumen de la estrategia

- Solo se procesan velas finalizadas de 30 minutos. Dos bloques Previous value y seis Converter extraen Open y Close de la vela actual y de las dos anteriores, de modo que cada barra comprueba una ventana móvil de tres velas rojas y verdes.
- Deben estar formados tanto ATR 14 como su media simple de 30 valores. La volatilidad alta usa la condición estricta `ATR > media de ATR × 0,8`; no se decide durante el calentamiento de los indicadores.
- Tres velas rojas válidas compran desde cero o invierten un corto. Tres velas verdes válidas venden desde cero o invierten un largo. La inversión cierra primero una unidad con ReduceOnly y abre una unidad en la nueva dirección solo cuando la Order de cierre queda totalmente ejecutada.
- Sin una inversión válida de alta volatilidad, tres velas verdes cierran un largo y tres rojas cierran un corto. Un contador con estado también cierra cualquier lado tras veinte barras finalizadas de tenencia; una inversión en la misma vela tiene prioridad sobre la salida temporal.
- Tres bloques Combination reúnen los dos motivos de salida larga, los dos de salida corta y los flujos de ejecución de las ocho acciones. Una ejecución bloquea acciones durante las doce velas finalizadas siguientes; la decimotercera es la primera elegible. No hay stop-loss, take-profit ni bloque de protección de posición.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando la vela actual y las dos velas finalizadas anteriores cierran por debajo de sus aperturas, ATR 14 está estrictamente por encima de `media de ATR × 0,8` y la pausa está lista, una posición plana envía una compra a mercado NoCondition por Order Volume 1. Desde un corto, primero se envía una compra a mercado ReduceOnly por 1; su Order totalmente ejecutada actualiza el volumen y activa la compra a mercado NoCondition por 1.
- **Entrada en corto**: Cuando la vela actual y las dos velas finalizadas anteriores cierran por encima de sus aperturas, ATR 14 está estrictamente por encima de `media de ATR × 0,8` y la pausa está lista, una posición plana envía una venta a mercado NoCondition por Order Volume 1. Desde un largo, primero se envía una venta a mercado ReduceOnly por 1; su Order totalmente ejecutada actualiza el volumen y activa la venta a mercado NoCondition por 1.
- **Salida**: Un largo se cierra con tres velas verdes consecutivas cuando el patrón no constituye a la vez una inversión de alta volatilidad, o cuando Max Hold Bars llega a 20. Un corto se cierra simétricamente con tres velas rojas o tras 20 barras. Los eventos de patrón y temporizador se reúnen por lado, y un Flag permite como máximo un cierre independiente por vela. Cada cierre independiente y ambas ejecuciones de una inversión escalonada alimentan la pausa común.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles Series | 00:30:00 | Serie de 30 minutos; solo las velas finalizadas actualizan patrones, indicadores, contadores, pausa y decisiones. |
| ATR Length | 14 | Periodo del indicador Average True Range que solo emite valores formados. |
| ATR Average Length | 30 | Periodo de la media móvil simple calculada sobre ATR; las decisiones esperan a que esta media esté formada. |
| ATR Multiplier | 0.8 | Multiplicador de la media de ATR. La volatilidad solo es válida cuando ATR supera estrictamente el umbral resultante. |
| Max Hold Bars | 20 | Número de barras finalizadas contadas mientras la posición no es plana antes de habilitar el cierre temporal. |
| Cooldown Bars | 12 | Número de velas finalizadas posteriores bloqueadas tras una secuencia de acciones; las decisiones se reanudan en la vela 13. |
| Order Volume | 1 | Cantidad fija para entradas desde cero, cierres ReduceOnly y entradas después de un cierre ejecutado; el diagrama está dimensionado para su propia posición de una unidad. |

## Detalles del diagrama

- El bloque [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite velas finalizadas de 30 minutos. Dos [Previous value](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) conservan los desplazamientos 1 y 2, y seis [Converter](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/converter.html) extraen los tres pares Open/Close.
- Seis [Comparison](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) clasifican cada vela con pruebas estrictas `Close < Open` y `Close > Open`. Dos [Logical condition](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) de tres entradas forman los patrones móviles rojo y verde; un doji hace falsos ambos.
- Dos [Indicator](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) de valores formados calculan ATR 14 y SMA 30 de ATR. Un bloque [Formula](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/formula.html) multiplica la media por 0,8 y una comparación estricta entrega la bandera de volatilidad alta.
- La [Position](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/current.html) se captura dos veces por vela: para enrutar operaciones y para el contador de tenencia. Los bloques [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) y Formula incrementan el contador solo con posición abierta y lo reinician mediante ejecuciones confirmadas.
- Dos [Combination](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/combination.html) booleanos reúnen salidas de patrón y temporizador sin contarlas ni modificarlas. Los [Flag](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/flag.html) por lado evitan cierres independientes duplicados, y las puertas de prioridad los suprimen cuando la misma vela ya cumple una inversión.
- Ocho bloques [Modify position](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) implementan dos entradas desde cero, dos salidas independientes y dos inversiones escalonadas. Cada inversión usa `ReduceOnly close 1 → fully matched Order → NoCondition open 1`; la Order ejecutada también vuelve a emitir el volumen en el mismo ciclo.
- Un Combination MyTrade envía cada ejecución de acción a una pausa [N values](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html). La primera ejecución inicia el conteo de doce velas y la segunda parte de la misma inversión no reinicia un conteo activo. El [Chart panel](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recibe velas, ATR, su media, el umbral y todas las ejecuciones.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
