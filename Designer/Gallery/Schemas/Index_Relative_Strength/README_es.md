# Diagrama de fuerza relativa frente a un instrumento de referencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama construye un instrumento sintético a partir del cociente entre el instrumento negociado y un instrumento de referencia, mide cuánto se ha alejado ese cociente de su propia media móvil simple de 60 periodos y opera el instrumento negociado según el resultado. Un cociente situado un uno por ciento por encima de su media significa que el instrumento negociado se comporta mejor que la referencia y el diagrama abre largo; un uno por ciento por debajo significa que se queda rezagado y el diagrama abre corto. Las órdenes se envían siempre al instrumento negociado; el instrumento sintético solo participa en la decisión.

![schema](schema.svg)

## Resumen de la estrategia

- Un bloque Security index construye un instrumento sintético a partir de la expresión `BTCUSDT@BNBFT/TONUSDT@BNBFT`. Sus velas son el precio de un instrumento expresado en unidades del otro, de modo que una serie creciente significa que el instrumento negociado gana terreno frente a la referencia.
- Las velas de cinco minutos ya finalizadas de ese instrumento sintético alimentan un Converter que lee el precio de cierre y una SimpleMovingAverage de longitud 60, que solo entrega valores ya formados y equivale a cinco horas de la misma serie.
- Una Formula divide el cierre del cociente entre su media y resta uno, obteniendo la fuerza relativa como fracción: `+0.01` significa que el cociente está un uno por ciento por encima de su media de cinco horas, y `-0.01`, un uno por ciento por debajo.
- Una serie aparte de velas de cinco minutos finalizadas del instrumento negociado marca el ciclo de decisión. Un bloque Variable, cuya entrada se usa solo como almacenamiento, guarda la última fuerza relativa y la libera con la vela negociada, de manera que cada comparación y cada orden llevan la marca temporal de la barra negociada y no la de la sintética.
- Dos bloques Comparison contrastan el valor liberado con el umbral y con su negativo, obtenido mediante una Formula `0 - a`, de modo que un único número expuesto gobierna ambos lados de forma simétrica.
- La Position actual se compara dos veces con cero, lo que da `Position <= 0` y `Position >= 0`. Dos bloques Logical condition combinan cada señal de fuerza con la comprobación de posición correspondiente, de modo que un lado ya abierto no puede recibir otra entrada.
- Sin posición, un bloque Position modify configurado como Open position compra o vende a mercado el Order Volume fijo. No hay bloque de stop-loss ni de take-profit.
- Una posición abierta en contra de la señal nueva se cierra primero mediante un bloque Position modify configurado como Close position, lo que deja la reversión para la siguiente vela que cumpla la condición.

## Reglas de entrada y salida

- **Entrada en largo**: En una vela negociada finalizada, cuando la fuerza relativa liberada es mayor que el Strength Threshold y la posición no es larga, se activa la puerta larga. Sin posición, el bloque Open position compra a mercado el Order Volume. Desde una posición corta la entrada se rechaza en esa barra, porque lo que se ejecuta primero es el cierre; el largo se abre en la siguiente vela que siga mostrando un mejor comportamiento relativo.
- **Entrada en corto**: En una vela negociada finalizada, cuando la fuerza relativa liberada es menor que el negativo del Strength Threshold y la posición no es corta, se activa la puerta corta. Sin posición, el bloque Open position vende a mercado el Order Volume. Desde una posición larga la entrada se rechaza en esa barra, porque lo que se ejecuta primero es el cierre; el corto se abre en la siguiente vela que siga mostrando un peor comportamiento relativo.
- **Salida**: No hay regla de salida independiente, ni stop-loss ni take-profit. Una posición solo se abandona ante una señal en sentido contrario: un mejor comportamiento relativo cierra un corto y uno peor cierra un largo. La acción de cierre toma su volumen de la posición abierta, de modo que después la cuenta queda sin posición, y el lado opuesto se abre en la vela siguiente si la señal continúa presente.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | Expresión con la que se construye el instrumento sintético. El primer instrumento es el numerador y el segundo, el denominador de referencia, de modo que la serie sube cuando el numerador gana terreno frente a la referencia. Cámbiala para medir el instrumento negociado contra otra referencia. |
| Ratio Candles | 00:05:00 | Marco temporal de las velas del instrumento sintético. Debe coincidir con el de la serie negociada, porque la fuerza liberada es un valor por cada barra negociada. |
| Traded Candles | 00:05:00 | Marco temporal de las velas del instrumento negociado. Cada comparación, cada entrada y cada salida se evalúan una vez por cada vela finalizada de esta serie. |
| Reference Average Length | 60 | Número de barras de la media móvil simple del cociente. Sesenta barras de cinco minutos miden la fuerza de las últimas cinco horas; una media más larga mide una divergencia más lenta y menos frecuente, y produce menos operaciones. |
| Strength Threshold | 0.01 | Distancia respecto a la media, expresada como fracción, que el cociente debe recorrer antes de tomar un lado. `0.01` es un uno por ciento, aplicado por encima de la media para las entradas largas y por debajo de ella para las cortas. Redúcelo para operar con más frecuencia; auméntalo para exigir una divergencia más amplia. |
| Order Volume | 1 | Cantidad fija de cada entrada. Las acciones de cierre la ignoran y toman su volumen de la posición abierta. |

## Detalles del diagrama

- El bloque [Security index](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/index.html) contiene la expresión con la que se construye el instrumento sintético y alimenta la entrada Security de un bloque [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html). Un segundo bloque Candles, dejado en el instrumento de la estrategia, suministra la serie negociada. Ambos están configurados para entregar solo velas finalizadas, de modo que ninguna barra en formación pueda fechar una orden en la apertura de su propia barra.
- Un [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html) lee el precio de cierre de la vela sintética y un [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) que solo entrega valores formados promedia la misma serie sobre 60 barras. La [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) `a / b - 1` convierte ese par en una única fracción con signo, y una segunda Formula `0 - a` refleja el umbral en el lado débil.
- Una vela sintética se compone a partir de dos flujos de datos y se completa después de una vela ordinaria del mismo minuto. Por eso un [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) almacena la fuerza en su entrada y la emite solo con el disparador de la vela negociada, que es lo que mantiene las marcas temporales de las órdenes en el reloj del instrumento negociado. Las constantes del umbral, del cero y del volumen de la orden se disparan desde esa misma vela para que cada [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) vea sus dos operandos dentro de una misma evaluación.
- La [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) se compara con cero en cada vela negociada, y dos bloques [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) unen el resultado de la fuerza con el de la posición. Solo un resultado `true` llega a un bloque de trading; una comparación `false` se descarta en la entrada del disparador.
- Cuatro bloques [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) actúan mediante órdenes de mercado. Los dos bloques de entrada usan la condición Open position, por lo que actúan solo con la cuenta sin posición y no pueden repetirse mientras un lado está abierto. Los dos bloques de salida usan la condición Close position, que calcula su volumen a partir de la posición abierta y no hace nada cuando la cuenta ya está sin posición o ya está en el lado solicitado.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
