# Diagrama de la estrategia de divergencia de spread de dos patas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos instrumentos que suelen moverse juntos a veces se mueven en distinta medida, y el que se queda atrás tiende a recuperar terreno. El diagrama mide cuánto ha recorrido cada uno de los dos a lo largo del mismo número de barras, resta una cifra de la otra y, cuando la brecha se abre lo suficiente, compra la pata rezagada y vende la pata adelantada en el mismo momento. Ambas patas se devuelven cuando la brecha vuelve a cerrarse.

![schema](schema.svg)

## Resumen de la estrategia

- Una variable Security nombra el segundo instrumento, de modo que el par es un ajuste y no una decisión de cableado, y esa misma variable es la que apunta hacia él la segunda serie de velas y cada bloque de órdenes de la segunda pata. La primera pata es el instrumento al que está configurada la propia estrategia.
- Ambas series de velas están configuradas para trabajar solo con velas finalizadas, de modo que una barra sin terminar nunca puede mover una decisión ni fechar una orden.
- Sync mantiene una línea por instrumento y deja salir ambas velas juntas al ritmo de cinco minutos. Los dos flujos llegan de forma independiente y solo después de esa retención las dos velas pertenecen a la misma barra, que es lo único que hace significativa la comparación entre los dos instrumentos.
- Por cada vela liberada un conversor toma el cierre, un bloque Previous value guarda la vela un número fijo de barras atrás y un segundo conversor lee el cierre de esa vela más antigua. El bloque guarda la vela en sí y el campo se lee después, que es el orden que da un valor en cada barra.
- Dos fórmulas convierten cada par de precios en un único número: cuánto ha recorrido esa pata desde la barra más antigua, en porcentaje del precio antiguo.
- Dos variables enganchan esos dos porcentajes a la vela negociada: cada una guarda la última cifra que produjo el par y la libera cuando termina una barra del instrumento negociado, de modo que cada comparación, cada compuerta y cada orden aguas abajo lleva el reloj del instrumento al que van las órdenes.
- Una fórmula resta el recorrido de la segunda pata al de la pata negociada, que es la divergencia, y otra toma su magnitud sin signo para la salida. Las comparaciones contrastan la divergencia con una banda simétrica, las dos cifras de las patas con cero y la posición con cero; dos And y un Or convierten las dos lecturas de signo en un único flag de correlación.
- Tres condiciones lógicas arman las decisiones y seis bloques de posición las ejecutan: dos abren un par en cada dirección, una orden por instrumento, y dos cierran ambas patas. Cada bloque de apertura está configurado para actuar solo desde una posición plana en su propio instrumento, de modo que una señal que se repite mientras un par está abierto no le añade nada.

## Reglas de entrada y salida

- **Entrada en largo**: La divergencia se sitúa por debajo del borde inferior de la banda —la pata negociada se ha quedado rezagada respecto a la segunda—, ambas patas recorrieron el mismo sentido a lo largo del desplazamiento y la pata negociada está plana. Con esa única señal dos bloques se disparan a la vez: uno compra a mercado el volumen de la pata negociada en el instrumento de la estrategia, el otro vende a mercado el volumen de la segunda pata en el instrumento indicado.
- **Entrada en corto**: La divergencia se sitúa por encima del borde superior de la banda —la pata negociada se ha adelantado a la segunda—, ambas patas recorrieron el mismo sentido a lo largo del desplazamiento y la pata negociada está plana. El par de bloques espejo vende el volumen de la pata negociada y compra el volumen de la segunda pata, ambos a mercado.
- **Salida**: Ambas patas se devuelven a la vez cuando la brecha con la que se abrieron se ha cerrado: la magnitud de la divergencia, tomada sin signo, cae por debajo del umbral de salida mientras hay un par abierto. Dos bloques de cierre se disparan con esa única señal, uno por instrumento, y cada uno calcula su propio tamaño a partir de lo que encuentra abierto, de modo que ninguno necesita un volumen propio y un bloque de cierre disparado sobre un instrumento plano simplemente no hace nada. Aquí no hay objetivo de dinero, ni stop-loss, ni límite de tiempo; que las dos patas vuelvan a juntarse es toda la salida.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Second Instrument | TONUSDT@BNBFT | El instrumento en el que se negocia la segunda pata. Debe existir en los datos conectados y ser un instrumento realmente negociable: se le envían órdenes, no solo se lee de él. |
| Traded Candles | 00:05:00 | Longitud de las velas del propio instrumento de la estrategia. Las decisiones y las órdenes van a este ritmo. |
| Second Leg Candles | 00:05:00 | Longitud de las velas del segundo instrumento. Mantenla igual que la serie negociada, o las dos patas se miden sobre lapsos de tiempo distintos y la diferencia entre ellas no significa nada. |
| Sync Interval | 00:05:00 | El ritmo al que la retención libera ambos instrumentos. Ajústalo a la longitud de la vela. |
| Traded Leg Shift | 12 | Cuántas barras atrás se mide el recorrido de la pata negociada. Doce barras de cinco minutos son una hora. |
| Second Leg Shift | 12 | El mismo conteo para la segunda pata. Mantén ambos iguales: dos lapsos distintos hacen que la resta carezca de sentido. |
| Divergence Threshold, % | 0.3 | Cuánto tienen que separarse los dos recorridos, en porcentaje, para que valga la pena abrir el par. La banda es simétrica, así que este único valor fija ambos bordes. |
| Exit Threshold, % | 0.1 | Cuánto tienen que volver a acercarse los dos recorridos, en porcentaje, para que se devuelvan ambas patas. Mantenlo por debajo del umbral de entrada, o un par se cerrará en la misma barra en que se abrió. |
| Traded Leg Volume | 1 | Tamaño de la orden en el instrumento de la estrategia, en lotes. |
| Second Leg Volume | 10 | Tamaño de la orden en el segundo instrumento, en lotes. Los dos tamaños se fijan de forma independiente, así que la proporción entre las patas es un ajuste y no algo calculado a partir de los precios: elígela para que encaje con las escalas de precio de los dos instrumentos y compruébala contra el paso de volumen del segundo, o la orden se redondeará a la baja. |

## Detalles del diagrama

- Sync es lo que empareja las barras, pero nada se ordena a partir de su liberación. Dos flujos no terminan en el mismo instante, y un conjunto liberado por la retención lleva la más temprana de las dos horas; una orden fechada antes de la hora actual se rechaza. Las dos variables que enganchan las cifras a la vela negociada son las que mantienen todo lo posterior en un único reloj.
- Vale la pena conocer la consecuencia de ese enganche: las cifras sobre las que se toma una decisión son el último par completo, así que el par se abre en la barra siguiente a la que produjo la lectura, y no dentro de ella.
- Toda constante —el cero, ambos umbrales y ambos volúmenes— se dispara con la vela negociada. Una comparación necesita que sus dos valores vuelvan a llegar en cada evaluación, y una constante que nunca se reenvía detiene en silencio la condición que alimenta, sin señal alguna de ello.
- El borde inferior de la banda es una fórmula sobre el umbral y no una segunda constante, de modo que la banda se mantiene simétrica sea cual sea el valor del umbral, y hay un solo número que cambiar en lugar de dos que pueden desajustarse.
- El bloque de posición lee únicamente el instrumento negociado, y su valor se engancha a la barra del mismo modo que las cifras de las patas. Ambas patas se abren y se cierran con las mismas señales, así que el estado de la pata negociada representa el estado del par.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
