# Diagrama de la estrategia Open Drive
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama toma una única vela de impulso: aquella cuyo cuerpo es mayor que una fracción del Average True Range actual. El color de ese cuerpo decide el lado, la SMA 20 tiene que coincidir con él, el reloj tiene que estar dentro de las primeras seis horas del día UTC y la posición tiene que estar plana. Un take profit y un stop loss son la única salida.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cinco minutos terminadas suministran Close y Open a través de dos bloques Converter e impulsan la SMA 20 y el ATR 14. Ambos indicadores trabajan en modo solo formado, así que ninguna comparación produce un veredicto hasta que cada uno ha reunido suficientes velas.
- Un bloque Formula mide el cuerpo de la vela actual como `abs(Close - Open)`; un segundo convierte el ATR actual en un umbral, `ATR x 0.3`. Un bloque Comparison califica la vela como impulso cuando el cuerpo es estrictamente mayor que ese umbral, de modo que la barra sobre la que actúa el diagrama resulta inusualmente grande para la volatilidad del momento.
- Dos bloques Comparison leen el color de esa misma vela, `Close > Open` y `Close < Open`, y otros dos leen de qué lado queda respecto de la SMA 20, `Close > SMA` y `Close < SMA`. El impulso por sí solo nunca opera: el color y la tendencia tienen que apuntar en la misma dirección.
- El bloque Current time transmite la hora de la estrategia a un bloque Working time que cubre de 00:00:00 a 06:00:00 UTC. Su respuesta verdadero/falso se guarda en un bloque Variable que la vuelve a publicar cuando llega una vela, de modo que el filtro de sesión se decide en el mismo tick que todas las comparaciones de precio y no según un reloj propio.
- Un bloque Variable toma una instantánea de la posición en cada vela y un bloque Comparison contra cero indica si el diagrama está plano. Leer la posición a través de una instantánea evita que una ejecución que llegue entre dos velas reactive la lógica de entrada en mitad de la barra.
- El bloque Logical condition largo es `impulso Y cuerpo alcista Y cierre por encima de la SMA Y dentro de la ventana Y posición plana`; el corto es su reflejo. Cada uno espera las cinco entradas, por lo que publica exactamente un veredicto por cada vela terminada.
- Un veredicto verdadero dispara un bloque Modify position en modo OpenPosition, que envía una orden a mercado por el volumen configurado y la rechaza salvo que la posición sea realmente cero. Por eso una misma vela nunca puede abrir dos operaciones, y una posición abierta bloquea por completo las nuevas entradas.
- Las ejecuciones de entrada de ambos lados pasan por un bloque Combination hacia Position protection, que arma un take profit del 3% y un stop loss del 2% frente al precio de cierre de cada vela terminada posterior.

## Reglas de entrada y salida

- **Entrada en largo**: Dentro de 00:00:00-06:00:00 UTC, con la SMA 20 y el ATR 14 formados y la posición plana: `abs(Close - Open) > ATR x 0.3`, `Close > Open` y `Close > SMA 20` envían una compra a mercado OpenPosition de una unidad.
- **Entrada en corto**: Dentro de 00:00:00-06:00:00 UTC, con la SMA 20 y el ATR 14 formados y la posición plana: `abs(Close - Open) > ATR x 0.3`, `Close < Open` y `Close < SMA 20` envían una venta a mercado OpenPosition de una unidad.
- **Salida**: No hay salida por señal ni reversión. Position protection cierra la operación con un 3% de beneficio o un 2% de pérdida respecto del precio de ejecución de entrada, evaluado en el cierre de cada vela terminada, de modo que un pico intrabarra que atraviese un nivel no se atiende hasta que esa vela está completa. El diagrama no mantiene ningún contador de espera entre operaciones: una vez cerrada la posición, la siguiente vela que cumpla las condiciones dentro de la ventana puede abrir otra.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de las velas terminadas; todas las comparaciones, ambos indicadores y los niveles de protección se evalúan sobre sus cierres. |
| MA Period | 20 | Período de la media móvil simple, en modo solo formado, que decide en qué lado de la tendencia ha cerrado una vela. |
| ATR Period | 14 | Período del Average True Range, en modo solo formado, que describe el tamaño de vela normal del momento. |
| ATR Multiplier | 0.3 | Fracción del ATR actual que el cuerpo de una vela debe superar para contar como impulso. Subirla exige velas más raras y más grandes; bajarla admite velas corrientes. |
| Window Begin | 00:00:00 | Inicio de la ventana de negociación, en UTC. Antes de ella, los impulsos se miden y se dibujan, pero nunca se opera con ellos. |
| Window End | 06:00:00 | Fin de la ventana de negociación, en UTC. Amplía el par a 00:00:00-23:59:59 para que el diagrama opere las veinticuatro horas. |
| Order Volume | 1 | Cantidad enviada por ambas entradas; la posición siempre es de una unidad porque una segunda entrada se rechaza mientras está abierta. |
| Take Profit | 3% | Distancia del take profit, como porcentaje del precio de ejecución de entrada. |
| Stop Loss | 2% | Distancia del stop loss, como porcentaje del precio de ejecución de entrada. |

## Detalles del diagrama

- El bloque [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite velas de cinco minutos terminadas, que el histórico de minutos incluido puede construir. Dos bloques [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) leen Close y Open, y dos bloques [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) en modo solo formado calculan la SMA 20 y el ATR 14.
- Dos bloques [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) construyen el cuerpo y el umbral del ATR, y cinco bloques [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) convierten cuerpo, color, lado de la tendencia y posición en señales.
- El bloque [Current time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) alimenta la hora de la estrategia hacia [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html), cuya respuesta cambia mucho más a menudo que una vela. Un bloque [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) con Input as trigger desactivado guarda esa respuesta y la libera solo cuando la siguiente vela lo dispara.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) se captura como instantánea mediante un segundo bloque Variable y se compara con una constante cero. Ambos bloques [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) de cinco entradas esperan todas ellas, por lo que cada vela produce un veredicto largo y un veredicto corto.
- Dos bloques [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) en modo OpenPosition operan a mercado. Un bloque [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) une las ejecuciones de entrada de ambos lados para [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html), y el [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) dibuja las velas, la SMA, el ATR, todas las órdenes incluida la pareja protectora y todas las ejecuciones.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
