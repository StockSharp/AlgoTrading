# Diagrama de la estrategia Early Bird Range Latch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama opera una ruptura estricta del extremo de la vela anterior de cinco minutos cuando el precio coincide con la dirección de la EMA 20. Un cierre diario UTC admite como máximo una posición nueva por día, mientras que el ATR 14 actual define los límites de stop y objetivo.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas terminadas de cinco minutos suministran el cierre actual, el máximo y el mínimo de la vela anterior, la EMA 20 y el ATR 14. Los bloques Previous value desplazan solo los flujos High y Low, por lo que la decisión nunca compara una vela con sus propios extremos.
- La configuración larga exige `Close > previous High` y `Close > EMA 20`; la corta exige `Close < previous Low` y `Close < EMA 20`. Todas las comparaciones son estrictas, así que la igualdad no genera una señal.
- Un bloque Time controla el intervalo fijo de reinicio diario desde 00:00:00 hasta 00:04:59 UTC. La hora de la vela controla el intervalo fijo de entrada desde 00:05:00 hasta 23:59:59, y un Flag compartido deja pasar solo la primera configuración direccional válida tras cada reinicio.
- Una configuración aceptada guarda el cierre actual como precio de entrada y abre una unidad a mercado solo cuando la instantánea de posición es cero. El cierre permanece consumido después de salir de la posición e impide otra entrada hasta el siguiente reinicio UTC.
- En cada vela terminada posterior, las fórmulas recalculan cuatro límites desde la entrada guardada y el ATR actual: stop y objetivo largos en `entry − 1.5×ATR` y `entry + 2.5×ATR`, con los signos invertidos para un corto. Las acciones ReduceOnly a mercado cierran el lado correspondiente al alcanzar cualquiera de los límites.

## Reglas de entrada y salida

- **Entrada en largo**: Una vez formada la EMA 20, entre 00:05:00 y 23:59:59 UTC, una posición plana, `Close > previous High`, `Close > EMA 20` y un Flag diario disponible envían una compra OpenPosition a mercado de una unidad.
- **Entrada en corto**: Una vez formada la EMA 20, entre 00:05:00 y 23:59:59 UTC, una posición plana, `Close < previous Low`, `Close < EMA 20` y un Flag diario disponible envían una venta OpenPosition a mercado de una unidad.
- **Salida**: Para un largo, se activa una venta ReduceOnly a mercado con `Close ≤ entry − 1.5×current ATR` o `Close ≥ entry + 2.5×current ATR`. Para un corto, se activa una compra ReduceOnly a mercado con `Close ≥ entry + 1.5×current ATR` o `Close ≤ entry − 2.5×current ATR`. No hay salida horaria, seguimiento, reversión ni reentrada en el mismo día.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de las velas terminadas que activan todos los cálculos de señal y riesgo. |
| EMA Length | 20 | Periodo de la media móvil exponencial formada que se usa como filtro direccional. |
| ATR Length | 14 | Periodo del Average True Range formado que se recalcula para cada vela terminada. |
| Stop ATR Multiplier | 1.5 | Multiplicador aplicado al ATR actual para situar el límite adverso respecto al precio de entrada guardado. |
| Target ATR Multiplier | 2.5 | Multiplicador aplicado al ATR actual para situar el límite favorable respecto al precio de entrada guardado. |
| Order Volume | 1 | Cantidad fija suministrada a las dos entradas OpenPosition y a las dos salidas ReduceOnly. |

## Detalles del diagrama

- El bloque [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite velas terminadas de cinco minutos y puede construirlas a partir del historial por minutos incluido.
- Tres [Converters](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html) extraen Close, High y Low. Dos bloques [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) aplican Shift 1 a los flujos numéricos High y Low.
- Los bloques [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) con salida solo tras formarse calculan EMA 20 para la dirección y ATR 14 para la distancia de riesgo. La disponibilidad de EMA también impide entradas antes de que ambos indicadores tengan datos suficientes.
- El flujo [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/time.html) alimenta el bloque de reinicio [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html). Otro bloque Working time lee la hora de la vela y participa directamente en ambas condiciones de entrada.
- Un [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) compartido consume el primer candidato largo o corto del día UTC. Los bloques Variable capturan la posición y el cierre de la entrada aceptada; una segunda variable de precio de entrada vuelve a publicar el valor guardado en cada vela para las fórmulas de riesgo.
- Los bloques [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html), [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) y [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) construyen los filtros de ruptura y los cuatro límites ATR. Los Flags de salida por vela evitan cierres duplicados cuando varias entradas se actualizan en una evaluación.
- Dos bloques OpenPosition y dos ReduceOnly de [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) gestionan entradas y salidas a mercado. El [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recibe velas, High y Low anteriores, EMA, ATR y un flujo Combination con todas las ejecuciones.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
