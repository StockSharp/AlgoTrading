# Diagrama de la estrategia Recovery Target Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Las entradas aquí dependen de una sola cosa: una vela mucho más amplia que las que el mercado ha venido formando últimamente. La salida se decide por dinero y no por precio: el resultado abierto de la posición se compara con un objetivo que el diagrama guarda en una variable, y ese objetivo no es constante. Una salida perdedora lo multiplica, una salida ganadora lo devuelve a la cifra base, de modo que cada operación sabe cuánto costó la anterior.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de quince minutos ya finalizadas alimentan cuatro conversores que extraen el máximo, el mínimo, la apertura y el cierre de cada barra.
- Una fórmula resta el mínimo al máximo para obtener el rango de la barra, mientras que un indicador Average true range mide cuál ha sido ese rango en las últimas diez barras.
- Una segunda fórmula multiplica el average true range por el multiplicador de ruptura, y una comparación pregunta si el rango de esta barra supera ese umbral: en eso consiste toda la definición de barra anormalmente amplia.
- La dirección se resuelve con una sola comparación: cierre por encima de la apertura. Un Not lógico convierte esa misma señal en el caso de barra bajista, de modo que ambas entradas leen el mismo cuerpo de vela desde lados opuestos.
- Ambas puertas de entrada son un And lógico de tres términos: la barra es amplia, apunta en la dirección correcta y la posición está plana. Position modify compra o vende entonces a mercado con el volumen de la orden.
- P&L change entrega el resultado abierto de la posición en cada actualización, y dos comparaciones lo miden frente al objetivo en dinero y frente al stop en dinero.
- El objetivo en dinero no es una cifra fija: una fórmula multiplica el objetivo base por un factor de recuperación guardado en una variable, de modo que la meta se mueve con el estado de recuperación en lugar de estar escrita dos veces en el diagrama.
- El factor de recuperación lo reescriben exactamente dos eventos, cada uno a través de su propia puerta, y un Combination une ambas escrituras en la única entrada de la variable que lo almacena.

## Reglas de entrada y salida

- **Entrada en largo**: Una vela finalizada cuyo rango de máximo a mínimo supera el average true range multiplicado por el multiplicador de ruptura y que cierra por encima de su propia apertura, tomada desde posición plana. Position modify compra a mercado con el volumen de la orden.
- **Entrada en corto**: Una vela finalizada cuyo rango supera ese mismo umbral pero que no cierra por encima de su propia apertura, tomada desde posición plana. Position modify vende a mercado con el mismo volumen de la orden.
- **Salida**: En el diagrama no hay stop por precio ni objetivo por precio: la posición se cierra solo por dinero. Cuando el resultado abierto alcanza el objetivo en dinero vigente, se dispara un Position modify configurado para cerrar la posición; cuando cae hasta el stop en dinero, se dispara un segundo. Cada bloque de cierre es dueño de una rama del cerrojo de recuperación: la ejecución del cierre perdedor libera el factor aumentado, la ejecución del cierre ganador libera la cifra uno, y ambas escrituras se encuentran en un Combination que alimenta la variable que guarda el factor.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:15:00 | Marco temporal de las velas con las que trabaja todo el diagrama. |
| ATR Length | 10 | Número de barras sobre las que se mide el average true range; es el patrón con el que se juzga si una barra es amplia. |
| Breakout Multiplier | 1.5 | Cuántas veces más amplia que la media tiene que ser una barra para contar como ruptura. Súbelo para entradas más raras y extremas, bájalo para obtener más. |
| Volume | 1 | Tamaño de cada orden de entrada. Todas las cifras de dinero que siguen son resultado de ese tamaño, así que cambiar una obliga a reajustar las demás. |
| Target Base | 300 | Objetivo en dinero de una operación tomada después de una ganadora, en la moneda en la que se cuenta el resultado. |
| Recovery Multiplier | 2 | Por cuánto se multiplica el objetivo tras una operación perdedora. Dos significa que la siguiente operación tiene que recuperar el doble del objetivo base; uno desactiva la recuperación y deja un objetivo en dinero simple. |
| Stop Money | -600 | Resultado abierto al que se abandona una posición, escrito como número negativo. Es una cifra fija y el factor de recuperación no la escala. |

## Detalles del diagrama

- El cerrojo es el único bucle del diagrama: la variable del factor alimenta una fórmula que la multiplica por el multiplicador de recuperación, el resultado espera en una variable de puerta, y la puerta lo escribe de vuelta en la variable del factor cuando un cierre perdedor se ejecuta realmente.
- Ambas puertas se disparan con la ejecución de un bloque de cierre, no con la comparación que pidió ese cierre. Una comparación puede repetir su veredicto varias veces mientras la orden de cierre sigue en camino; una ejecución ocurre una sola vez, así que el factor se multiplica una vez por cada operación perdedora.
- La variable del factor toma su entrada sin tratarla como disparador, de modo que una escritura solo cambia lo que almacena. Emite con el disparador que se le ha dado, que es la actualización de P&L, y eso mantiene ambos lados de la comparación del objetivo en el mismo reloj.
- La rama del dinero permanece en silencio hasta que se abre la primera posición, porque el resultado abierto solo se informa cuando hay algo que valorar. A partir de ese momento el factor, el objetivo base y el multiplicador de recuperación se liberan juntos en cada actualización.
- Las velas se suscriben únicamente como finalizadas, de modo que cada señal pertenece a una barra ya cerrada y las órdenes llevan la hora de ese cierre y no la hora en que se abrió la barra.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
