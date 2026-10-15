# Diagrama de la estrategia Z-Score de spread delta neutral
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos instrumentos que normalmente se mueven juntos a veces se separan, y la distancia entre ellos tiende a cerrarse de nuevo. El diagrama divide un precio por el otro para obtener un instrumento sintético, mide cuánto se ha alejado ese cociente de su propia media en unidades de su propia volatilidad y abre ambos instrumentos a la vez en direcciones opuestas: largo en el lado barato contra corto en el caro. Cuando el cociente vuelve a donde suele situarse, se liberan las dos patas. Cada entrada se anuncia además como una línea de texto que lleva la lectura que la provocó.

![schema](schema.svg)

## Resumen de la estrategia

- Un bloque de índice construye un único instrumento sintético a partir de los dos reales dividiendo el precio del primero por el precio del segundo, y se suscribe una serie de velas a ese instrumento sintético, de modo que el spread llega en forma de velas ya hechas en lugar de montarse a mano a partir de dos flujos de datos.
- Una media móvil y una desviación estándar recorren las velas del spread, y un conversor toma su precio de cierre. Esas tres cifras son todo lo que necesita la decisión: dónde está el spread, dónde suele situarse y con qué amplitud suele oscilar.
- Una fórmula las convierte en un z-score, la distancia del cierre a la media dividida por la desviación, y una fórmula reflejada produce la misma cifra con el signo cambiado, de modo que un único umbral de entrada y un único umbral de salida sirven para ambas direcciones sin un segundo par de constantes.
- Otras dos series de velas corren sobre los dos instrumentos que realmente se negocian, y dos variables enganchan las lecturas al instrumento negociado: cada una guarda el último número que produjo el spread y lo libera cuando termina una vela del instrumento negociado, de modo que cada decisión lleva el reloj del instrumento al que van las órdenes.
- Dos bloques de posición, uno por instrumento, informan de lo que está abierto. Sus valores se enganchan en la misma barra del instrumento negociado y se comparan con cero, lo que da al diagrama cuatro respuestas simples: cada pata está sin posición o abierta.
- Las comparaciones contra el umbral de entrada dicen si el spread está muy por debajo o muy por encima de su media, y una condición lógica lo combina con que ambas patas estén sin posición. Solo entonces se disparan a la vez cuatro bloques de posición, comprando un instrumento y vendiendo el otro por el mismo tamaño.
- Dos comparaciones contra el umbral de salida, unidas por una condición lógica, dicen que la lectura es pequeña en términos absolutos, es decir, que el spread ha vuelto cerca de su media. Cada pata tiene su propia puerta de liberación, de modo que a una pata que ya está sin posición nunca se le pide cerrar y a una que sigue abierta siempre se le pide.
- En el momento de la entrada una variable captura el z-score que la provocó, un formateador de cadenas lo escribe en una frase y un bloque de notificación pone esa frase en el registro de la estrategia. El panel del gráfico dibuja los dos instrumentos negociados, la serie sintética del spread, la media, la desviación y todas las órdenes y ejecuciones.

## Reglas de entrada y salida

- **Entrada en largo**: El z-score reflejado está por encima del umbral de entrada, lo que significa que el spread se sitúa más de esa cantidad de desviaciones estándar por debajo de su propia media, y ambas patas están sin posición. El diagrama compra el instrumento negociado y vende el instrumento de cobertura, los dos a mercado y los dos por el volumen de la orden.
- **Entrada en corto**: El z-score está por encima del umbral de entrada, lo que significa que el spread se sitúa más de esa cantidad de desviaciones estándar por encima de su propia media, y ambas patas están sin posición. El diagrama vende el instrumento negociado y compra el instrumento de cobertura, los dos a mercado y los dos por el volumen de la orden.
- **Salida**: Ambas patas se liberan cuando el z-score absoluto cae por debajo del umbral de salida, es decir, cuando el spread ha vuelto a una banda estrecha alrededor de su media. Los dos bloques de cierre no llevan un lado propio y están configurados para cerrar la posición: cada uno decide por sí mismo en qué dirección operar y por cuánto, de modo que el mismo par de bloques deshace tanto un spread largo como uno corto. Además, la pata negociada lleva un stop-loss porcentual medido desde su precio de ejecución. Ese stop cubre una sola pata: una posición emparejada no puede cerrarse con una única orden de protección, así que la pata de cobertura mantiene su propia puerta de liberación y se cierra cuando el spread vuelve.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | La expresión a partir de la cual se construye el instrumento sintético: el precio del instrumento negociado dividido por el precio del instrumento de cobertura. Los dos instrumentos tienen que existir en los datos conectados, y los dos se negocian. |
| Spread Candles | 00:05:00 | Longitud de las velas sobre las que se construye la serie del spread y, por tanto, la barra sobre la que se miden la media y la desviación. |
| Traded Leg Candles | 00:05:00 | Longitud de las velas sobre las que se sigue la pata negociada. Este es el reloj por el que corre todo el diagrama, así que manténla igual a la de la serie del spread o las lecturas enganchadas serán más antiguas que la barra en la que se leen. |
| Hedge Leg Candles | 00:05:00 | Longitud de las velas sobre las que se sigue la pata de cobertura. La serie existe para que los precios del instrumento de cobertura lleguen a la ejecución y aparezcan en el gráfico; manténla igual a las otras dos. |
| Average Length | 20 | Número de velas del spread en la media móvil desde la que se mide el z-score. |
| Deviation Length | 20 | Número de velas del spread en la desviación estándar por la que se divide el z-score. Manténlo igual a la longitud de la media salvo que quieras deliberadamente un nivel rápido frente a una amplitud lenta. |
| Entry Z-Score | 1.5 | Cuántas desviaciones estándar tiene que alejarse el spread de su media antes de abrir el par. Subirlo hace que las entradas sean más raras y que el tramo en el que se toman sea más amplio. |
| Exit Z-Score | 0.5 | Cuánto tiene que acercarse el spread a su media antes de liberar ambas patas. Es una cifra absoluta y cubre los dos lados, así que el mismo número cierra tanto un spread largo como uno corto. |
| Order Volume | 1 | Tamaño de cada pata, en lotes. Las dos patas se envían por el mismo tamaño. |
| Stop Loss, % | 1.5 | Stop-loss de la pata negociada, en porcentaje del precio al que se ejecutó. Protege una sola pata; la pata de cobertura no tiene stop propio. |

## Detalles del diagrama

- El instrumento sintético es un cociente y no una diferencia. Una diferencia entre dos instrumentos con niveles de precio muy distintos queda dominada por el mayor, y el z-score mediría entonces ese único instrumento en lugar de la relación entre los dos.
- Las dos series no terminan en el mismo instante: un instrumento sintético se monta a partir de dos flujos de datos y su vela se cierra un poco después que la de uno normal. Enganchar las lecturas a la vela del instrumento negociado mantiene todo el diagrama en un solo reloj; liberar ambas series a la vez fecharía cada orden con la más temprana de las dos, y una orden fechada antes de la hora actual se rechaza.
- Las constantes se disparan con la vela del instrumento negociado. Una comparación necesita que sus dos valores vuelvan a llegar en cada evaluación, así que una constante que nunca se reenvía detiene en silencio la condición a la que alimenta.
- La división que produce el z-score tiene como suelo un número positivo muy pequeño, de modo que un tramo de precios perfectamente quietos no puede dividir por una desviación cero y detener la ejecución.
- Cada pata la vigila su propio bloque de posición y se controla por separado, de modo que si el stop saca primero a la pata negociada, la pata de cobertura sigue liberándose por su propia condición en lugar de quedarse abierta hasta la siguiente entrada.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
