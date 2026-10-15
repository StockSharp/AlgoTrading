# Diagrama de estrategia de comparación de MACD entre instrumentos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama opera un instrumento y decide sobre dos. Se mide un MACD sobre el instrumento operado y sobre un instrumento de referencia, y lo que se compara no son sus precios sino su impulso: cuando el lado operado es el más débil de los dos en la lectura rápida y a la vez sigue siendo el más fuerte en la lectura lenta, el diagrama lo interpreta como una diferencia que todavía no se ha cerrado y lo compra. La imagen especular vende.

![schema](schema.svg)

## Resumen de la estrategia

- El bloque Index construye un instrumento sintético a partir de una expresión y se lo entrega a un segundo bloque de velas, que es la manera en que un segundo instrumento entra siquiera en un diagrama.
- Ambas patas se suscriben como series de velas del mismo marco temporal: una sobre el instrumento de la estrategia y otra sobre el instrumento que ha producido Index.
- Sync mantiene una línea por pata y deja salir las dos juntas, de modo que una lectura del instrumento operado y una lectura del instrumento de referencia describen siempre la misma barra y no la serie que haya llegado antes por casualidad.
- Cada pata recibe su propio MACD, y los convertidores extraen de él tres números: la línea MACD, la línea de señal y el precio de cierre de la vela subyacente.
- Dos fórmulas por pata convierten esos números en porcentajes del precio de esa misma pata: el histograma, MACD menos señal, y la propia línea de señal. Comparar los valores brutos carecería de sentido cuando los dos instrumentos cotizan a precios con órdenes de magnitud de diferencia.
- Cuatro variables leen los cuatro porcentajes en la vela operada, de modo que tanto la comparación como la orden que la sigue se acompasan al instrumento que se opera y no a la pata que haya cerrado en último lugar.
- Las comparaciones ponen las patas una junto a otra — histograma contra histograma, señal contra señal — y cuatro condiciones lógicas añaden el estado de la posición, una compuerta por acción: abrir largo, abrir corto, cerrar largo, cerrar corto.
- Las entradas son órdenes a mercado de volumen fijo que solo se toman desde una posición plana; la lectura opuesta cierra, y Position protection lleva un take-profit y un stop-loss en porcentaje del precio de entrada.

## Reglas de entrada y salida

- **Entrada en largo**: En una vela operada, el histograma del instrumento operado se sitúa por debajo del histograma de referencia mientras que su línea de señal se sitúa por encima de la línea de señal de referencia, y la posición está plana. La lectura rápida dice que el lado operado va por detrás, la lectura lenta dice que todavía va por delante, y el diagrama lee el par como un retraso que está por recuperarse. Position modify compra el volumen de la orden a mercado.
- **Entrada en corto**: En una vela operada, el histograma del instrumento operado se sitúa por encima del histograma de referencia mientras que su línea de señal se sitúa por debajo de la línea de señal de referencia, y la posición está plana. Position modify vende el volumen de la orden a mercado.
- **Salida**: Hay dos formas de salir y cualquiera de ellas puede llegar primero. La lectura especular cierra la posición: un largo se cierra cuando el histograma se adelanta al de referencia y la línea de señal se queda por detrás de él, y un corto con el par opuesto. Cada una es un Position modify configurado para cerrar, así que el volumen procede de la propia posición y nada se invierte dentro de una misma orden: el diagrama vuelve a plano y espera una señal nueva. Con independencia de eso, Position protection sigue a cada ejecución de entrada y cierra con un 1.6% de beneficio o un 0.8% de pérdida respecto del precio de entrada.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Reference Security | TONUSDT@BNBFT * 1 | Expresión a partir de la cual el bloque Index construye el instrumento de referencia. Nombra un instrumento y lo multiplica por uno, lo que deja el precio intacto y mantiene el ajuste como una expresión aritmética. |
| Traded Candles | 00:15:00 | Marco temporal de la serie de velas operada, y el compás sobre el que se construye cada orden. |
| Reference Candles | 00:15:00 | Marco temporal de la serie de velas de referencia. Tiene que coincidir con el operado, o las dos patas nunca caerán en la misma barra. |
| Sync Interval | 00:15:00 | Duración del grupo en el que Sync reúne las dos patas; la misma duración que la de las velas. |
| Traded MACD Fast | 12 | Longitud de la media móvil rápida del MACD construido sobre el instrumento operado. |
| Traded MACD Slow | 26 | Longitud de la media móvil lenta del mismo MACD. La diferencia entre ella y la longitud rápida decide cuánto tiene que durar un movimiento antes de que el histograma reaccione. |
| Traded MACD Signal | 9 | Longitud de la línea de señal del mismo MACD; es la línea contra la que se mide el histograma. |
| Reference MACD Fast | 12 | Longitud de la media móvil rápida del MACD construido sobre el instrumento de referencia. Cada pata lleva sus propias tres longitudes, de modo que las dos pueden ajustarse por separado, pero una comparación de los dos histogramas solo significa algo mientras estén configuradas igual. |
| Reference MACD Slow | 26 | Longitud de la media móvil lenta del MACD de referencia. Mantenla igual que la del operado salvo que los dos instrumentos deban medirse en horizontes distintos. |
| Reference MACD Signal | 9 | Longitud de la línea de señal del MACD de referencia. Mantenla igual que la del operado por la misma razón. |
| Order Volume | 1 | Tamaño de la orden, en lotes. |
| Take Profit, % | 1.6 | Distancia del take-profit, en porcentaje del precio de entrada. |
| Stop Loss, % | 0.8 | Distancia del stop-loss, en porcentaje del precio de entrada. |

## Detalles del diagrama

- Ambos bloques de velas toman únicamente velas finalizadas. Una actualización de una vela en formación lleva la hora de apertura de la barra, y una orden construida a partir de ella sería más antigua que el momento en que se envía.
- Sync libera un conjunto bajo la hora más temprana de los valores que contiene, razón por la cual nada de Sync llega directamente a una orden: las cuatro variables vuelven a leer los valores en la vela operada, y es el compás de esa vela el que sirve de base a las órdenes. El coste es que una comparación utiliza el último par completo de lecturas, una barra por detrás de la vela sobre la que actúa.
- Toda variable que guarda una constante — el cero y el volumen de la orden — se dispara con la vela operada. Una variable sin disparador conserva su valor y nunca lo emite, y una condición que la esperase nunca llegaría a completarse.
- Ambos bloques MACD emiten solo valores formados y definitivos, así que la comparación comienza en cuanto cada pata tiene detrás un indicador completo y no puede moverse dentro de una barra.
- Los dos extremos de cada línea de Sync están conectados. Un valor que entra y nunca se extrae deja el bloque esperándolo, y la estrategia no arrancará.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
