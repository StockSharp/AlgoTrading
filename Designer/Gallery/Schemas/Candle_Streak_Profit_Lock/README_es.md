# Diagrama de la estrategia Candle Streak Profit Lock
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Cuatro velas que cierran todas del mismo modo forman una racha, y este diagrama opera en la dirección de la racha. Entrar es la mitad fácil; la mitad interesante es salir, porque el diagrama no espera a un nivel de precio para decidirlo. Vigila el dinero de la posición abierta y, la primera vez que esa cifra alcanza un importe fijado, la posición se cierra y el beneficio queda asegurado. Un enclavamiento garantiza que esa orden se envíe una sola vez y no en cada actualización del resultado.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cuatro horas ya finalizadas se leen una sola vez y se reutilizan: la vela actual pasa directamente a un conversor de cierre y a otro de apertura, y tres bloques Previous value devuelven las velas de una, dos y tres barras atrás, cada una con su propio conversor de cierre y de apertura.
- Ocho comparaciones convierten esos cuatro pares en el color de cada barra: cierre por encima de la apertura es una vela alcista, cierre por debajo de la apertura es una vela bajista, y una barra que cierra exactamente donde abrió no es ninguna de las dos, así que falla ambas pruebas y no puede prolongar ninguna racha.
- Un AND lógico reúne las cuatro respuestas alcistas y un segundo reúne las cuatro bajistas; cada uno emite verdadero solo cuando las cuatro barras de la ventana coinciden, que es en lo que consiste una racha.
- El bloque de posición comparado con cero indica si el diagrama está fuera del mercado, largo o corto, y cada puerta aguas abajo es un AND lógico de una racha y un estado de la posición.
- Ambas entradas son órdenes a mercado de volumen fijo y llevan la condición sobre la posición abierta, de modo que una racha que sigue corriendo mientras ya hay una posición abierta no puede añadir una segunda orden encima de la primera.
- Las ejecuciones de entrada se unen en una sola línea y se entregan a Position protection, que lleva un take-profit y un trailing stop como porcentaje del precio de ejecución y los cotiza mientras la posición siga viva.
- El bloque Strategy P&L emite el resultado abierto de la posición en curso con su propio ritmo, y una comparación contrasta esa cifra con el bloqueo de beneficio (Profit Lock); la respuesta va a un bloque Flag, que deja pasar el primer verdadero y luego queda fijado, de modo que se envía exactamente una orden de cierre.
- Una racha que se forma en contra de una posición abierta la cierra a mercado, y el Flag se rearma desde cualquiera de las dos puertas de entrada, así que cada nueva posición empieza otra vez con su bloqueo listo.

## Reglas de entrada y salida

- **Entrada en largo**: Cuatro velas finalizadas seguidas cierran por encima de sus propias aperturas mientras no hay posición abierta: Position modify compra el volumen de la orden a mercado.
- **Entrada en corto**: Cuatro velas finalizadas seguidas cierran por debajo de sus propias aperturas mientras no hay posición abierta: Position modify vende el volumen de la orden a mercado.
- **Salida**: Tres formas de salir, y la posición se va por la que llegue primero. El bloqueo de beneficio la cierra en cuanto el resultado abierto alcanza el importe fijado; como el Flag ya ha dejado pasar esa señal una vez, las actualizaciones posteriores del mismo resultado no envían nada. Position protection la cierra por su take-profit o por su trailing stop, un stop que sigue al precio en cuanto la operación entra en beneficio. Una racha en dirección contraria también la cierra a mercado, y esa orden de cierre es todo lo que ocurre en esa barra: las puertas de entrada exigen que no haya posición abierta, así que un giro lo abre la siguiente señal de racha que encuentre el diagrama vacío, no la misma que lo vació.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Type | 04:00:00 | Marco temporal de la serie de velas sobre la que se cuenta la racha; las cuatro barras de una racha son cuatro velas de esta duración. |
| Order Volume | 1 | Tamaño de la orden, en lotes, que envía cada entrada; también escala el resultado abierto con el que se compara el bloqueo de beneficio. |
| Take Profit | 1% | Distancia del take-profit respecto al precio de ejecución, en porcentaje. |
| Stop Loss | 1% | Distancia del stop-loss respecto al precio de ejecución, en porcentaje; el stop es dinámico, así que sigue al precio en cuanto la operación se mueve a favor y nunca retrocede. |
| Profit Lock | 200 | Resultado abierto, en el dinero de la cartera, al que se cierra la posición y se asegura el beneficio. |

## Detalles del diagrama

- La longitud de la racha está incorporada en el propio diagrama en vez de fijarse como un número: cuatro barras significan tres bloques Previous value y cuatro entradas en cada puerta de racha. Una racha de cinco son tres bloques más y una entrada más por puerta; una racha de tres, un bloque menos.
- Cada bloque Previous value está tipado como vela y lee la serie de velas directamente, y el cierre y la apertura se toman de lo que devuelve. Tomar primero un precio y pedir después su valor anterior es el orden inverso, y es el que deja la comparación sin operando.
- El bloqueo de beneficio es una cifra en el dinero de la cartera, no una distancia en precio, así que depende tanto del volumen de la orden como del instrumento. Duplicar el volumen reduce a la mitad el movimiento necesario para alcanzarlo; cambiar a un instrumento de otra escala de precios lo cambia por completo.
- El bloqueo y el take-profit son dos respuestas a la misma pregunta y gana el más ajustado. Ajusta el bloqueo para que se dispare antes que el take-profit y el take pasa a ser el techo que solo se alcanza cuando una barra salta por encima del bloqueo; ponlo amplio y el take-profit es la salida habitual y el bloqueo, la red de seguridad que queda detrás.
- Todo lo que envía el diagrama son órdenes a mercado, y la serie de velas se suscribe solo con barras finalizadas. Una señal construida sobre una barra que todavía se está formando lleva la hora de apertura de esa barra, y una orden marcada con una hora ya pasada se rechaza.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
