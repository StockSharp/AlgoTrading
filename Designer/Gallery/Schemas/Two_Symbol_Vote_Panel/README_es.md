# Diagrama de la estrategia de panel de votación sobre dos instrumentos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un panel de votación sobre dos instrumentos. Cada hora cerrada, cada instrumento se puntúa frente a su propia hora anterior con siete lecturas de precio —apertura, máximo, mínimo, cierre, punto medio, precio típico y cierre ponderado— y cada lectura que sube emite un voto al alza y cada una que baja, un voto a la baja. Las dos puntuaciones se suman en un único veredicto, y el instrumento negociado se compra o se vende solo en función de ese veredicto. No hay ningún indicador en todo el diagrama: la decisión entera se construye con precios de velas, aritmética y comparaciones.

![schema](schema.svg)

## Resumen de la estrategia

- Dos bloques de velas alimentan el diagrama. Uno funciona sobre el instrumento propio de la estrategia y es el que se negocia; el otro apunta a un instrumento concreto mediante una variable Security, de modo que el segundo símbolo es un ajuste y no una decisión de cableado.
- Ambas series toman únicamente velas cerradas. Un voto contado sobre una barra aún en formación cambiaría varias veces dentro de la hora y fecharía la orden a la que lleva con la apertura de esa hora.
- En cada serie, Previous value retiene la vela completa un paso atrás, y los conversores leen apertura, máximo, mínimo y cierre de la vela actual y de la retenida. Primero se retiene la vela y después se leen sus campos: ese es el orden que devuelve un valor en cada barra.
- Esos ocho números entran en una Formula por instrumento. Se suman siete términos sign(), uno por lectura, que devuelven +1, 0 o -1 cada uno: el resultado son los votos al alza menos los votos a la baja y queda entre -7 y +7. sign es lo que hace posible una comparación dentro de una fórmula, que por sí sola no tiene forma de devolver un verdadero o un falso.
- La puntuación de referencia se muestrea sobre la barra negociada mediante una Variable con la entrada como disparador desactivada: la puntuación llega a la entrada y espera allí, la vela negociada llega al disparador y la deja salir. Dos flujos nunca actualizan en el mismo instante, y esto es lo que pone el segundo instrumento en el reloj del instrumento que se negocia.
- Una segunda Formula suma las dos puntuaciones en el veredicto del panel, entre -14 y +14, y el veredicto se dibuja en el gráfico bajo las velas junto con las dos puntuaciones que lo componen.
- Un único umbral gobierna ambos lados. Una comparación contrasta el veredicto con él, una fórmula de una sola línea le invierte el signo y una segunda comparación contrasta el veredicto con ese valor: así, subir el ajuste endurece por igual el caso largo y el corto.
- La posición se fija a la barra mediante una variable y se compara con cero de tres maneras: sin posición admite una entrada; larga o corta admite una salida. Cuatro puertas AND accionan cuatro bloques Position modify —dos abren y dos cierran— y un segundo panel del gráfico dibuja el instrumento de referencia junto al negociado.

## Reglas de entrada y salida

- **Entrada en largo**: En una barra cerrada el veredicto del panel está por encima del umbral —los dos instrumentos juntos emiten al alza una mayoría clara de sus catorce votos— y no hay posición abierta. Se dispara la puerta larga y Position modify compra el volumen de la orden a mercado bajo la condición Open position, de modo que una barra posterior que repita el veredicto no añade nada a la posición.
- **Entrada en corto**: El caso simétrico: el veredicto está por debajo del umbral negado, la mayoría de los catorce votos apunta a la baja y no hay posición abierta. Se dispara la puerta corta y el segundo Position modify vende el volumen de la orden a mercado, de nuevo bajo la condición Open position.
- **Salida**: No hay stop-loss ni take-profit. El mismo panel que abrió la posición es el que la devuelve: un veredicto por debajo del umbral negado mientras la posición es larga dispara la puerta de salida, y Close position devuelve toda la posición a mercado; un veredicto por encima del umbral mientras la posición es corta hace lo mismo en el otro lado. Como los bloques de cierre cierran lo que está abierto en lugar de vender un tamaño fijo, una salida nunca puede dar la vuelta a la posición: un giro del panel deja primero el diagrama sin posición, y la siguiente barra que siga leyendo igual abre el nuevo lado.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Reference Instrument | TONUSDT@BNBFT | El segundo instrumento cuya barra emite la otra mitad de los votos; debe estar disponible en la conexión junto con el negociado. |
| Traded Candles | 01:00:00 | Marco temporal de las velas negociadas: el compás con el que se lee el veredicto y se envían las órdenes. |
| Reference Candles | 01:00:00 | Marco temporal de las velas de referencia. Mantenlo igual al de las negociadas; de lo contrario, las dos puntuaciones se cuentan sobre periodos de distinta duración y la suma deja de significar lo que dice. |
| Vote Threshold | 2 | Qué mayoría deben emitir los dos instrumentos antes de abrir una posición, y cuánto debe girar el veredicto en sentido contrario antes de devolverla. Se cuenta en votos, sobre los catorce que emite el panel. |
| Order Volume | 1 | Tamaño de la orden, en lotes. |

## Detalles del diagrama

- Todos los bloques de órdenes están configurados para no exigir una conexión en línea, de modo que el diagrama se comporta igual sobre historial que sobre un flujo en vivo; con su valor por defecto, el bloque retendría todas las transacciones sobre datos reproducidos.
- Las entradas llevan la condición Open position y las salidas la condición Close position con el lado opuesto. Sin una condición, un bloque actuaría ante cada cambio de posición que lo dispare, y un solo veredicto se convertiría en un flujo de órdenes.
- Todas las constantes —el cero, el umbral y el volumen de la orden— se disparan con la vela negociada. Una variable emite en su disparador y no cuando se establece su valor, de modo que una constante sin disparar dejaría muda durante toda la ejecución la comparación contigua.
- Una lectura que se repite exactamente no emite ningún voto: sign devuelve cero cuando el valor no cambia, así que solo se cuenta el movimiento real, y una hora plana en un instrumento simplemente deja que decida el otro.
- El umbral es lo que convierte el panel de un gatillo hipersensible en un filtro. En cero, el diagrama actúa ante la mayoría más leve y da la vuelta casi en cada barra; elevado, espera a que los dos instrumentos coincidan con suficiente fuerza, y la posición se mantiene a través de las discrepancias intermedias.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
