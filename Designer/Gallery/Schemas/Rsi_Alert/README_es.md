# Diagrama de estrategia de alertas RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama convierte los extremos del RSI en operaciones y alertas legibles. Procesa velas de cinco minutos finalizadas, compra con valores de 30 o inferiores y vende con valores de 70 o superiores solo cuando la posición está cerrada, y aplica protección porcentual a cada entrada ejecutada. Cada señal aceptada también captura el valor numérico del RSI, le da formato y escribe una notificación.

![schema](schema.svg)

## Resumen de la estrategia

- Un único flujo de velas de cinco minutos, solo finalizadas, alimenta el indicador, la instantánea de la posición, las decisiones de entrada, las comprobaciones del precio de protección y el gráfico.
- RelativeStrengthIndex usa un período de 14. Su filtro de valores formados está desactivado (`IsFormed = false`), por lo que los valores del período de calentamiento no se suprimen únicamente porque el indicador aún no esté formado.
- Un bloque Formula con la expresión `a` convierte el IndicatorValue del RSI en un valor decimal que utilizan las comparaciones y los mensajes.
- El RSI decimal se compara con los niveles de sobreventa y sobrecompra. La señal de cada dirección se combina con una instantánea de la posición tomada durante la evaluación de la vela actual, y ambos bloques de entrada usan la condición Open position.
- Las entradas ejecutadas activan la protección de posición con un take profit del 2% y un stop loss del 1%. Las señales de entrada aceptadas también pasan el valor RSI capturado por un formateador hasta una notificación Log.

## Reglas de entrada y salida

- **Entrada en largo**: El RSI decimal está en el Oversold Level o por debajo y la instantánea de la posición indica que está cerrada. El diagrama compra a mercado el volumen configurado y escribe una alerta de compra con el valor de la señal.
- **Entrada en corto**: El RSI decimal está en el Overbought Level o por encima y la instantánea de la posición indica que está cerrada. El diagrama vende a mercado el volumen configurado y escribe una alerta de venta con el valor de la señal.
- **Salida**: La protección de posición cierra la operación cuando el cierre de una vela finalizada alcanza el nivel de take profit del 2% o el nivel de stop loss del 1% respecto a la entrada. Una señal RSI opuesta no invierte una posición abierta, y una ejecución protectora no puede provocar otra entrada en la misma vela.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| RSI Period | 14 | Número de velas utilizadas para calcular RelativeStrengthIndex. |
| Oversold Level | 30 | Los valores RSI iguales o inferiores a este nivel permiten una entrada en largo mientras el diagrama no tenga una posición abierta. |
| Overbought Level | 70 | Los valores RSI iguales o superiores a este nivel permiten una entrada en corto mientras el diagrama no tenga una posición abierta. |
| Take Profit | 2% | Distancia protectora del take profit respecto al precio de entrada. |
| Stop Loss | 1% | Distancia protectora del stop loss respecto al precio de entrada. |
| Volume | 0.01 | Volumen de la orden de entrada, en lotes. |
| Candles | 00:05:00 | Marco temporal de velas de cinco minutos; solo se procesan las velas finalizadas. |

## Detalles del diagrama

- La salida de [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) activa primero la instantánea de la posición actual, después actualiza el bloque [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) y finalmente actualiza un [Convertidor](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) del precio de cierre.
- La salida del RSI entra en un bloque [Formula](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/formula.html) cuya expresión es `a`. Su salida decimal llega a ambas memorias del valor del mensaje antes de que se evalúe cualquiera de las comparaciones de umbral.
- Dos bloques de [Comparación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) comprueban el RSI decimal frente a los valores compartidos Oversold Level y Overbought Level mediante `<=` y `>=`.
- El valor de [Posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/current.html) se mantiene en una [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) durante la evaluación de la vela actual y se compara con cero. Dos bloques de [Condición lógica](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) combinan ese resultado de posición cerrada con las señales RSI de largo y corto.
- Ambos bloques de entrada [Modificar posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) usan órdenes de mercado con la condición Open position y reciben `0.01` de un único valor de volumen compartido.
- Las salidas MyTrade de ambos bloques de entrada alimentan la [Protección de posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/protect.html). El convertidor del cierre de vela suministra su entrada Price, y el bloque usa un take profit del 2% y un stop loss del 1%.
- Cada señal de entrada combinada activa su propia memoria del valor RSI. [Formato de cadena](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) genera `RSI {0:0.0} <= 30 — buy` o `RSI {0:0.0} >= 70 — sell`, y los bloques de [Notificación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) de tipo Log publican los mensajes.
- El panel del gráfico recibe velas finalizadas, valores RSI, ambos flujos de operaciones de entrada y las operaciones de salida protectoras.

## Uso

Importe el archivo `.json` en Designer y ejecútelo sobre datos históricos en el backtester. Observe en el registro las notificaciones RSI formateadas y compruebe las salidas protectoras frente a los cierres de las velas. Si cambia alguno de los umbrales RSI, actualice también su plantilla de formato para que el texto de la alerta siga siendo exacto.
