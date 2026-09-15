# Diagrama de la estrategia de desviación del spread entre dos instrumentos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos instrumentos que normalmente se mueven juntos a veces dejan de hacerlo, y la distancia entre ellos tiende a cerrarse de nuevo. El diagrama divide un precio entre el otro, observa cuánto se aleja ese cociente de su propia media móvil y compra o vende el primer instrumento en el momento en que la distancia empieza a cerrarse, no en el momento en que se abre.

![schema](schema.svg)

## Resumen de la estrategia

- Un bloque de índice construye un único instrumento sintético a partir de dos reales dividiendo el precio del primero entre el precio del segundo, y se suscribe una serie de velas a ese instrumento sintético, de modo que el spread llega en forma de velas ya hechas en lugar de componerse a mano.
- Una media móvil recorre las velas del spread y un conversor toma su precio de cierre, lo que da al spread tanto un nivel actual como una línea de referencia propia.
- Una fórmula reduce el par a un solo número: la distancia entre el cierre y la media, en porcentaje de la media.
- La misma lectura una barra atrás se reconstruye a partir de una vela anterior del spread y un valor anterior de la media, y una segunda fórmula convierte esos dos en la desviación de la barra anterior.
- Una segunda serie de velas corre sobre el instrumento negociado, y dos variables fijan a ella las dos desviaciones: cada una conserva el último número que produjo el spread y lo libera cuando se cierra una vela del instrumento negociado, de modo que toda decisión se toma según el reloj del instrumento al que van las órdenes.
- Las comparaciones plantean entonces cuatro preguntas: dónde estaba la desviación anterior respecto al umbral, hacia dónde se mueve la desviación ahora, en qué lado de la media sigue estando y si la posición está plana. Una condición lógica reúne las cuatro respuestas en una única puerta de entrada por dirección.
- Las entradas son órdenes a mercado de tamaño fijo tomadas solo desde posición plana, y se envían para el instrumento que la propia estrategia tiene configurado. El instrumento sintético es una fuente de números y nunca es objeto de ninguna orden.
- Dos comparaciones más vigilan que la desviación vuelva a alcanzar la media y entregan ese momento a dos bloques de posición, uno para cada lado, que cierran lo que esté abierto.

## Reglas de entrada y salida

- **Entrada en largo**: La desviación de la barra anterior estaba por debajo del umbral inferior, la desviación de esta barra es mayor que la anterior, la desviación sigue por debajo de la media y la posición está plana. El bloque de largos compra el volumen de la orden a mercado.
- **Entrada en corto**: La desviación de la barra anterior estaba por encima del umbral superior, la desviación de esta barra es menor que la anterior, la desviación sigue por encima de la media y la posición está plana. El bloque de cortos vende el volumen de la orden a mercado.
- **Salida**: Una posición se cierra cuando el spread termina el recorrido por el que fue abierta: un largo se cierra cuando la desviación alcanza la media o la cruza al alza, y un corto cuando la alcanza o la cruza a la baja. Cada bloque de cierre lleva su propio lado, de modo que el destinado a los largos no puede tocar un corto, y un bloque de cierre disparado sobre una posición plana simplemente no hace nada. Aquí no hay objetivo de beneficio, stop-loss ni límite de tiempo; el regreso del spread es toda la salida.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | La expresión a partir de la cual se construye el instrumento sintético: el precio del primer instrumento dividido entre el precio del segundo. Ambos instrumentos deben existir en los datos conectados, y solo el primero de ellos se negocia. |
| Spread Candles | 00:05:00 | Duración de las velas sobre las que se construye la serie del spread. |
| Traded Candles | 00:05:00 | Duración de las velas sobre las que se colocan las órdenes. Mantenla igual que la serie del spread, o los números fijados serán más antiguos que la barra en la que se leen. |
| Average Length | 20 | Número de velas del spread en la media móvil desde la que se mide la desviación. |
| Deviation Threshold, % | 0.3 | Cuánto tiene que alejarse el spread de su media, en porcentaje, para que merezca la pena operar un regreso hacia ella. |
| Order Volume | 1 | Tamaño de la orden, en lotes. |

## Detalles del diagrama

- La desviación de la barra anterior se reconstruye a partir de una vela anterior y un valor anterior de la media, en lugar de conservar el porcentaje ya calculado, de modo que ambas mitades del cociente se leen una barra atrás y la comparación entre el ahora y el entonces no puede mezclar dos momentos distintos.
- Las dos series no terminan en el mismo instante: un instrumento sintético se compone de dos flujos y su vela se cierra un poco después que la del instrumento simple. Fijar los números en la vela del instrumento negociado es lo que mantiene la estrategia en un solo reloj; liberar ambas series a la vez fecharía cada orden con la más temprana de las dos, y una orden fechada antes de la hora actual se rechaza.
- Las constantes se disparan con la vela del instrumento negociado. Una comparación necesita que sus dos valores lleguen de nuevo en cada evaluación, así que una constante que no se reenvía detiene la condición que alimenta sin dar ninguna señal de ello.
- El umbral inferior no es una segunda constante, sino una fórmula sobre el mismo valor, de modo que la banda se mantiene simétrica sea cual sea el umbral configurado.
- A los bloques de órdenes se les indica que no esperen a que la estrategia se ponga en línea, y las entradas están configuradas para abrir solo desde posición plana, de modo que una señal que se repite mientras hay una operación en curso no le añade nada.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
