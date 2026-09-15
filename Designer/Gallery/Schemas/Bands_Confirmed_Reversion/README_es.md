# Diagrama de la estrategia Bands Confirmed Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una vela que abre fuera de una banda de volatilidad y cierra de nuevo dentro de ella es la imagen clásica de un movimiento rechazado, y este diagrama compra y vende exactamente esa imagen. Lo que lo convierte en algo más que un patrón de una sola vela es lo que se interpone entre el patrón y la orden: un canal de precios cuyo borde debe haber mantenido su terreno, y un bloque de conteo que no libera su confirmación hasta que ha transcurrido un número fijado de velas. Solo cuando el patrón, el canal y el conteo coinciden en la misma vela se abre una posición.

![schema](schema.svg)

## Resumen de la estrategia

- Una única serie de velas de quince minutos alimenta todo el diagrama, y cada valor que entra en él pasa primero por un bloque Final, de modo que nada aguas abajo llega a ver una vela que todavía se está formando.
- Tres indicadores se calculan sobre ese flujo: bandas de volatilidad alrededor de una media móvil, un canal de precios de máximos y mínimos, y un rango verdadero medio que mide cuán ancha es una vela normal.
- Los convertidores separan las bandas en una línea superior y otra inferior, dividen el canal en un techo y un suelo, y extraen la apertura, el cierre, el máximo y el mínimo de cada vela cerrada.
- Dos bloques Previous value guardan los bordes del canal de la vela anterior, y dos comparaciones preguntan si el borde inferior ha dejado de caer y si el borde superior ha dejado de subir: esa es la definición que el diagrama da de un canal que aguanta.
- Cada una de esas dos respuestas arma un bloque N values, que a continuación cuenta el número configurado de velas cerradas y libera un único impulso de confirmación; un nuevo armado se ignora mientras hay un conteo en marcha.
- Una condición lógica reúne cinco cosas para el lado largo: la vela abrió por debajo de la banda inferior, cerró de nuevo por encima de ella, el suelo del canal aguanta en este momento, el impulso de confirmación acaba de llegar y la posición está plana. El lado corto es la misma condición reflejada en torno a la banda superior y el techo del canal.
- Ambas entradas son órdenes a mercado a través de bloques Position modify configurados para abrir solo desde posición plana, de modo que una señal que llegue mientras hay una operación en curso no puede acumular una segunda encima.
- Dos bloques Combination reúnen los motivos de salida —una distancia de stop construida a partir del rango verdadero medio y un cierre más allá del canal de la vela anterior— y los entregan a los bloques Position modify de cierre, mientras un panel de gráfico dibuja las velas, los tres indicadores, las órdenes y las ejecuciones.

## Reglas de entrada y salida

- **Entrada en largo**: Una vela cerrada abrió por debajo de la banda inferior y cerró de nuevo por encima de ella, el suelo del canal está en el mismo nivel o por encima del que tenía en la vela anterior, el bloque de conteo largo acaba de liberar su confirmación y la posición está plana. El bloque Position modify compra entonces el volumen de la orden a mercado. El conteo es lo que espacia las operaciones: tras cada liberación el bloque se vuelve a armar en la siguiente vela cuyo suelo del canal siga aguantando, de modo que el mismo patrón en la vela inmediatamente siguiente no produce una segunda entrada.
- **Entrada en corto**: La imagen especular. Una vela cerrada abrió por encima de la banda superior y cerró de nuevo por debajo de ella, el techo del canal está en el mismo nivel o por debajo del que tenía en la vela anterior, el bloque de conteo corto ha liberado su confirmación y la posición está plana. El bloque Position modify vende el volumen de la orden a mercado.
- **Salida**: Cada lado tiene su propio bloque Combination que sostiene dos tipos de motivo. El primero es un stop de volatilidad: el largo se cierra cuando el mínimo de la vela cae por debajo de la banda inferior menos el rango verdadero medio multiplicado por el multiplicador del stop, y el corto se cierra cuando el máximo de la vela sube por encima de la banda superior más esa misma distancia. Como la banda se mueve con el mercado, ese stop se desplaza por sí solo sin que el diagrama tenga que recordar un precio de entrada. El segundo motivo es una ruptura del canal: un cierre por encima del techo del canal de la vela anterior, o un cierre por debajo del suelo del canal de la vela anterior, termina la operación en cualquiera de los dos lados: al alza recoge el beneficio de un largo, a la baja corta su pérdida, y a la inversa para un corto. Ambos bloques de cierre están configurados para cerrar la posición, así que cada uno actúa solo sobre el lado al que pertenece y envía siempre el tamaño completo.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:15:00 | Marco temporal de la única serie de velas sobre la que funciona todo el diagrama. Un marco más corto da más patrones y más operaciones; uno más largo, menos y más lentas. |
| Bollinger Length | 100 | Número de velas sobre las que se promedian las bandas de volatilidad. También fija el calentamiento: no se opera nada hasta que haya transcurrido esa cantidad de velas. |
| Bollinger Width | 1 | A cuántas desviaciones típicas de la media se sitúa cada banda. Aquí se mantiene estrecha para que las velas abran con regularidad fuera de una banda y cierren de nuevo dentro; amplíela para obtener rechazos más raros y más extremos. |
| Donchian Length | 100 | Número de velas que abarca el canal de precios. Un canal largo hace lentos sus bordes, que es lo que convierte «el borde dejó de moverse en nuestra contra» en un filtro con sentido. |
| ATR Length | 21 | Número de velas sobre las que se mide el rango verdadero medio. Fija la unidad en la que se expresa la distancia del stop. |
| Long Confirm Candles | 5 | Velas que el bloque de conteo largo espera entre armarse y liberar su confirmación. Una vela elimina la espera por completo y opera todos los patrones; valores mayores adelgazan las entradas. |
| Short Confirm Candles | 5 | Velas que espera el bloque de conteo corto. Es un ajuste independiente para poder afinar los dos lados uno frente al otro. |
| ATR Stop Multiplier | 2 | A cuántos rangos verdaderos medios por debajo de la banda inferior se sitúa el stop del largo, y por encima de la banda superior el del corto. Redúzcalo para salidas más ajustadas y frecuentes. |
| Order Volume | 1 | Tamaño de la orden, en lotes, enviado en la entrada. Las salidas cierran siempre lo que esté abierto y no tienen tamaño propio. |

## Detalles del diagrama

- El bloque de velas está configurado solo para velas terminadas, y el bloque Final que va detrás impone la misma regla dentro del diagrama. Una actualización de una vela en formación lleva la hora a la que abrió la barra, y una orden construida a partir de ese valor queda fechada por detrás del reloj y se rechaza, de modo que toda la lógica se mantiene sobre velas cerradas.
- El bloque N values cuenta los valores que le llegan después de haber sido armado; no verifica que la condición de armado se haya mantenido cierta durante todo el intervalo. Por eso la misma comparación del canal que lo arma está también cableada directamente a la condición de entrada: el impulso dice que la espera ha terminado y la comparación en vivo dice si el motivo de esa espera sigue existiendo.
- Los bordes del canal usados para la salida se toman una vela atrás. El techo actual de un canal de máximos y mínimos ya contiene el máximo de la propia vela actual, así que un cierre nunca puede superarlo; frente al borde de la vela anterior, una ruptura sí es un hecho real.
- El stop está anclado a la banda de volatilidad y no al precio al que se abrió la operación. Un diagrama no guarda memoria de un precio de entrada salvo que se añada una variable para conservarlo, y la banda es de todos modos donde ocurrió la entrada, así que da la misma distancia de protección y se desplaza junto con el mercado.
- La posición se protege dos veces, a propósito: la comparación de posición plana está dentro de la condición de entrada, y los propios bloques de entrada están configurados para abrir solo cuando la posición es cero. Lo primero mantiene el diagrama legible; lo segundo es lo que de verdad impide una orden duplicada si ambas llegan desacompasadas.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
