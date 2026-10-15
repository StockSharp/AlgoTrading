# Orden límite en retroceso Fibonacci y SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama combina dos velocidades de Parabolic SAR con el rango de tres velas, mantiene como máximo una orden límite de retroceso Fibonacci, cancela la orden pendiente cuando se invierte su condición y cierra la posición ejecutada en niveles derivados del rango y guardados al entrar.

![schema](schema.svg)

## Descripción de la estrategia

- Las velas finalizadas de una hora de BTCUSDT alimentan los Parabolic SAR rápido y lento, Highest(3) y Lowest(3). Las decisiones comienzan cuando todos los indicadores están formados.
- La salida formada de Lowest libera un lote de decisión después de capturar Close, ambos SAR, máximo, mínimo, posición y estado de la orden pendiente de la vela actual.
- Un bloqueo global permite una sola orden de entrada activa. Se libera cuando la orden llega a su estado final, mientras que la posición muestreada impide otra entrada después de una ejecución.
- Las condiciones de entrada, cancelación y salida se guardan en acumuladores silenciosos y se liberan una vez por vela finalizada, sin mezclar valores de velas contiguas.
- El gráfico muestra velas, ambos SAR, el rango y los niveles de protección guardados, límites registrados y cancelados, salidas de mercado y todas las ejecuciones.

## Reglas de entrada y salida

- **Entrada larga**: Si `Slow SAR < Fast SAR < Close`, la posición está vacía y no hay entrada pendiente, se envía una orden límite Buy a `Low3 + (High3 - Low3) * 50%`. Se cancela antes de su ejecución si `Slow SAR > Fast SAR` o `Fast SAR >= Close`.
- **Entrada corta**: Si `Slow SAR > Fast SAR > Close`, la posición está vacía y no hay entrada pendiente, se envía una orden límite Sell a `High3 - (High3 - Low3) * 50%`. Se cancela antes de su ejecución si `Slow SAR < Fast SAR` o `Fast SAR <= Close`.
- **Salida**: Al aceptar la señal de entrada se guardan los niveles del lado correspondiente. Para un largo, el stop es `Low3 - 30` y el objetivo es `Low3 + (High3 - Low3) * 161%`; para un corto, el stop es `High3 + 30` y el objetivo es `High3 - (High3 - Low3) * 161%`. Cuando un Close finalizado alcanza cualquiera de los niveles guardados, se envía una orden de mercado opuesta de Volumen 1.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|---|---|---|
| Security | BTCUSDT@BNBFT | Instrumento usado por la suscripción de velas finalizadas. Configure Strategy Security con el mismo instrumento para órdenes y ejecuciones. |
| Candle Series | 01:00:00 | Velas finalizadas de una hora para indicadores, decisiones, salidas y gráfico. |
| Fast SAR Acceleration | 0.02 | Aceleración inicial del Parabolic SAR rápido. |
| Fast SAR Increment | 0.02 | Incremento de aceleración del Parabolic SAR rápido. |
| Fast SAR Maximum | 0.20 | Aceleración máxima del Parabolic SAR rápido. |
| Slow SAR Acceleration | 0.01 | Aceleración inicial del Parabolic SAR lento. |
| Slow SAR Increment | 0.02 | Incremento de aceleración del Parabolic SAR lento. |
| Slow SAR Maximum | 0.10 | Aceleración máxima del Parabolic SAR lento. |
| High Lookback | 3 | Número de velas finalizadas que Highest usa para `High3`. |
| Low Lookback | 3 | Número de velas finalizadas que Lowest usa para `Low3`. |
| Entry Fibonacci, % | 50 | Posición del precio límite dentro del rango actual de tres velas. |
| Target Fibonacci, % | 161 | Multiplicador del rango para cada objetivo de beneficio guardado. |
| Stop Offset | 30 | Distancia absoluta más allá del mínimo o máximo de tres velas para el stop guardado. |
| Order Volume | 1 | Cantidad de cada entrada y de cada salida de mercado protegida por lado. |

## Detalles del diagrama

- La [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) Security configura las [Candles](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) finalizadas; los bloques de transacción usan Strategy Security y Strategy Portfolio.
- Cuatro bloques [Indicator](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) que solo emiten valores formados calculan ambos Parabolic SAR y, por separado, el máximo y el mínimo de tres velas. La salida de Lowest es el reloj común del lote.
- Los bloques [Formula](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/formula.html), Variable y [Comparison](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) alinean las entradas numéricas, aplican protecciones de posición vacía y lado pendiente, y solo emiten impulsos de acción verdaderos.
- Cada señal aceptada guarda el stop y el objetivo calculados antes de activar su bloque [Order registering](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/orders/register.html). Los valores guardados no se mueven mientras la posición está abierta.
- La referencia de la orden registrada se conserva para una [Order cancellation](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html) dirigida. El bloqueo pendiente solo se libera con el evento Finished del bloque de registro después de una ejecución, cancelación confirmada o fallo de registro.
- Los bloques [Modify position](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html), protegidos por lado, envían una orden de mercado opuesta y fija de una unidad cuando el Close finalizado alcanza un stop u objetivo guardado. El gráfico recibe todos los flujos relevantes de precios, órdenes, cancelaciones y MyTrade.

## Uso

Importe el archivo `.json` en Designer, configure Strategy Security como BTCUSDT@BNBFT y ejecútelo sobre historial de una hora. Revise la escala de precios del instrumento, los niveles Fibonacci, el desplazamiento del stop, el ciclo de las órdenes y la salida de mercado antes de usar el diagrama en operativa real.
