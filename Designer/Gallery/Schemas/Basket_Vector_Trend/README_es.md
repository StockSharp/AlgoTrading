# Diagrama de estrategia de tendencia de cesta ponderada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama negocia dos instrumentos como una sola cesta y decide sobre tres series de precios en lugar de dos. A partir de ambos instrumentos negociados se construye un instrumento sintético, con un divisor dentro de su expresión que reduce la escala de la pata cara hasta que la barata todavía puede mover el resultado; la tendencia de ese instrumento sintético es lo que el diagrama entiende por dirección de la cesta. Después, cada pata se mide de la misma forma por separado. Una pata se compra o se vende solo cuando su propia tendencia coincide con la de la cesta, y cuando la cesta gira, se cierra toda pata que quede orientada en el sentido equivocado. Nada más cierra una posición: no hay objetivo ni stop, y el signo de la cesta es a la vez el motivo para estar dentro y el motivo para salir.

![schema](schema.svg)

## Resumen de la estrategia

- Un bloque de índice de instrumentos construye un único instrumento sintético a partir de los dos instrumentos negociados. El peso vive dentro de su expresión, de modo que las dos patas contribuyen en una escala comparable en vez de que el precio mayor ahogue al menor.
- Tres series de velas corren sobre el mismo marco temporal: una sobre la cesta sintética y una sobre cada pata. Las tres se suscriben únicamente como velas finalizadas, así que toda lectura posterior pertenece a una barra que ya ha cerrado.
- Cada serie alimenta una media móvil suavizada rápida y otra lenta, y una fórmula resta la lenta de la rápida. El resultado es una brecha de tendencia con signo, y hay tres: una para la cesta y una para cada pata.
- Cada brecha es capturada por una variable que la retiene y la libera cuando termina una vela de la primera pata, de modo que una decisión nunca se arma con una lectura de la cesta tomada en un momento y una lectura de la pata tomada en otro.
- El valor procede de la serie que lo haya producido en último lugar, el momento procede de la vela negociada y, por tanto, todo lo que viene después lleva la marca de tiempo de la barra en la que se envía la orden.
- Seis comparaciones convierten las tres brechas retenidas en banderas con signo frente a cero: cesta al alza o a la baja, primera pata al alza o a la baja, segunda pata al alza o a la baja.
- Dos bloques de posición, uno vinculado a cada instrumento, informan de lo que ya se mantiene, y otras dos comparaciones indican si esa pata está actualmente plana. Esto es lo que impide que una señal repetida apile una segunda entrada en la misma pata.
- Cuatro condiciones lógicas reúnen tres banderas cada una - dirección de la cesta, dirección de la pata, pata plana - y cada una dispara un bloque de orden a mercado. Otros cuatro bloques de órdenes se encargan de las salidas, gobernados directamente por las dos banderas de la cesta.

## Reglas de entrada y salida

- **Entrada en largo**: Una pata se compra cuando la brecha de la cesta es positiva, la brecha propia de esa pata es positiva y no se mantiene nada en esa pata. Las dos patas se deciden de forma independiente en la misma barra, así que ambas pueden estar largas a la vez, una puede estar larga mientras la otra se queda fuera, o puede que ninguna cumpla las condiciones.
- **Entrada en corto**: Una pata se vende cuando la brecha de la cesta es negativa, la brecha propia de esa pata es negativa y no se mantiene nada en esa pata. El lado corto es el espejo exacto del lado largo, y rige la misma independencia entre las patas.
- **Salida**: La única salida es un cambio en el signo de la brecha de la cesta. Una cesta negativa dispara los dos bloques de cierre que llevan dirección de venta, y una cesta positiva dispara los dos que llevan dirección de compra. La dirección en un bloque de cierre es un filtro y no una instrucción: el bloque actúa solo sobre una posición orientada en sentido contrario, de modo que un bloque de cierre del lado vendedor toca una pata larga y no hace absolutamente nada cuando esa pata está plana o ya corta. Una pata cuya propia brecha ha girado, pero cuya cesta no lo ha hecho, se deja en paz hasta que la cesta esté de acuerdo.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| First Leg | BTCUSDT@BNBFT | El primer instrumento negociado. Alimenta su propia serie de velas, su propio par de medias, su propio bloque de posición y sus propios cuatro bloques de órdenes; cambiarlo mueve esa pata entera. |
| Second Leg | TONUSDT@BNBFT | El segundo instrumento negociado, cableado igual que el primero. Las dos patas son simétricas y ninguna está subordinada a la otra. |
| Basket Index | BTCUSDT@BNBFT / 20000 + TONUSDT@BNBFT | La expresión con la que se construye el instrumento sintético de la cesta. El divisor es lo que pone las dos patas en una escala comparable - súbelo para dejar que domine la segunda pata, bájalo para dar más peso a la primera - y el signo de esta serie es lo que autoriza cada entrada. |
| Basket Candles | 00:15:00 | Marco temporal de las velas de la cesta. Mantenlo igual a los marcos temporales de las patas: los tres flujos están pensados para leerse como una sola barra. |
| First Leg Candles | 00:15:00 | Marco temporal de las velas de la primera pata. Esta serie es además el reloj de la negociación: dispara las retenciones, las constantes y, por tanto, el momento en que se envía cada orden. |
| Second Leg Candles | 00:15:00 | Marco temporal de las velas de la segunda pata. Se mantiene igual al de la primera pata para que ambas patas se juzguen sobre barras de la misma longitud. |
| Basket Fast Length | 3 | Longitud de la media rápida de la cesta. Más corta reacciona antes y cambia el signo de la cesta con más frecuencia, lo que abre y cierra posiciones más a menudo. |
| Basket Slow Length | 7 | Longitud de la media lenta de la cesta. La distancia entre esta y la longitud rápida fija cuán decidido tiene que ser un movimiento antes de considerar que la cesta ha girado. |
| First Leg Fast Length | 3 | Longitud de la media rápida de la primera pata. Solo decide si esa pata coincide con la cesta; nunca fija por sí misma la dirección de la cesta. |
| First Leg Slow Length | 7 | Longitud de la media lenta de la primera pata. Ampliar la distancia entre las dos longitudes hace que esta pata confirme con menos frecuencia, así que la cesta puede girar sin que ella se sume. |
| Second Leg Fast Length | 3 | Longitud de la media rápida de la segunda pata, que desempeña el mismo papel de confirmación para ese instrumento. |
| Second Leg Slow Length | 7 | Longitud de la media lenta de la segunda pata. Las dos patas pueden ajustarse de forma distinta a propósito si una de ellas es la más ruidosa del par. |
| First Leg Volume | 0.1 | Tamaño de una orden en la primera pata, en las unidades propias de ese instrumento. Conviene fijarlo junto con el volumen de la segunda pata para que una cesta completa ponga un dinero comparable en cada lado. |
| Second Leg Volume | 2000 | Tamaño de una orden en la segunda pata. Los dos volúmenes son separados porque los instrumentos cotizan en escalas completamente distintas, y un único número compartido haría trivial una de las patas. |

## Detalles del diagrama

- El peso que equilibra las dos patas forma parte de la expresión del índice, no es un número cableado en el diagrama. Reequilibrar la cesta significa editar esa única cadena de parámetro, y toda la serie sintética se reconstruye a partir de ella.
- Las velas se suscriben solo como finalizadas. Una orden enrutada desde una vela en formación lleva la marca de tiempo de la apertura de la barra, que es anterior al momento en que realmente se envía, y por ese motivo se rechaza; tomar únicamente barras cerradas mantiene cada orden sellada con el momento al que pertenece.
- Las tres variables de retención son lo que hace utilizable la cesta. Un instrumento sintético se ensambla a partir de dos flujos y su barra se completa más tarde que la de uno ordinario, así que un bloque que espera a que las tres lecturas caigan dentro de una misma ventana espera un conjunto que nunca llega. Retener cada lectura sobre la vela negociada toma el valor tal como está y el momento de la vela, y las comparaciones, las condiciones lógicas y las órdenes corren todas según el reloj de la barra negociada.
- Los bloques de entrada están configurados para abrir posición, así que una entrada actúa solo desde plano. Una señal que se mantiene cierta a lo largo de varias barras produce por tanto una orden y no una por barra, y los bloques de salida son la única vía de vuelta a plano.
- El panel del gráfico dibuja las tres series de velas, las seis medias móviles y todas las órdenes y ejecuciones de los ocho bloques de órdenes, de modo que una barra puede leerse frente a la cesta que la autorizó y la pata que la confirmó.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
