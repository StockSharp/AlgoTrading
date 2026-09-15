# Diagrama de la estrategia de filtro cada N barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos medias móviles exponenciales se cruzan y se compra un lado del mercado mientras se vende el otro. Lo importante de este diagrama es qué se les da a las medias. En lugar de leer todas las velas, leen un precio de cada quinta vela, y ese adelgazamiento lo hace un bloque N values conectado para contar el flujo de velas contra sí mismo. Todo lo que viene después — las medias, el cruce, las entradas — vive en ese reloj más lento, así que el diagrama mira el mercado una vez por ventana de muestreo y no una vez por barra.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cinco minutos ya cerradas alimentan un conversor que extrae el precio de cierre de cada vela y un bloque N values que convierte ese mismo flujo en un pulso de muestreo.
- El bloque N values recibe el flujo de velas por sus dos entradas a la vez: el disparador arma su cuenta atrás y la entrada la va descontando, de modo que emite un pulso en cada quinta vela cerrada y se vuelve a armar de inmediato.
- Ese pulso es el disparador de una Variable que mantiene el último precio de cierre. La variable guarda cada cierre según va llegando, pero no libera nada hasta que llega el pulso, así que lo que sale de ella es una serie de precios adelgazada: un valor por ventana de muestreo.
- Ambas medias móviles leen esa serie adelgazada en lugar de las velas, por lo que una media de catorce periodos abarca setenta velas de tiempo de mercado y una de cuarenta abarca doscientas.
- Un bloque Crossing vigila la media rápida frente a la lenta y solo habla en el momento en que las dos intercambian su posición: verdadero cuando la rápida cruza por encima, falso cuando cruza por debajo. Un NOT lógico convierte el caso descendente en una señal propia.
- El bloque Position se compara con una variable a cero mediante dos comparaciones — no está largo y no está corto — y cada resultado se une a su señal de cruce con un AND lógico, de manera que una entrada exige un cruce nuevo y una posición que no esté ya en ese lado.
- Ambos bloques de entrada están configurados solo para abrir, así que el diagrama lleva una única posición a la vez y nunca la incrementa; el volumen de la orden procede de una variable que se refresca en cada vela.
- El cruce contrario acciona dos bloques de cierre, Position protection se arma con cada ejecución y el panel del gráfico dibuja las velas, ambas medias, las órdenes de entrada y salida, las órdenes de protección y todas las ejecuciones.

## Reglas de entrada y salida

- **Entrada en largo**: La media rápida cruza por encima de la lenta en un pulso de muestreo y la posición no está larga. Position modify compra el volumen de la orden a mercado, solo en apertura, así que la entrada se toma con la cuenta plana y nunca se acumula sobre una operación abierta.
- **Entrada en corto**: La media rápida cruza por debajo de la lenta en un pulso de muestreo y la posición no está corta. El NOT lógico convierte el cruce descendente en una señal y Position modify vende el volumen de la orden a mercado, solo en apertura.
- **Salida**: Hay dos formas de salir. Position protection, armada por cada ejecución, cierra la operación con un 1.5% de beneficio o en un stop del 1%. Si no se alcanza ninguno de los dos antes de que las medias vuelvan a intercambiarse, toma el relevo el cruce contrario: dispara el bloque de cierre del lado que se mantiene y ese bloque envía la posición entera a mercado. Como los bloques de entrada solo abren, el cruce que termina una operación no abre la contraria: la siguiente entrada espera al siguiente cruce que llegue con la cuenta plana.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de las velas con las que trabaja todo el diagrama; la ventana de muestreo se cuenta en estas velas. |
| Bars Per Sample | 5 | Cuántas velas cerradas forman una muestra. Súbelo y las medias mirarán el mercado con menos frecuencia y operarán menos; ponlo a uno y el diagrama se convierte en un cruce corriente tomado en cada vela. |
| Fast EMA Length | 14 | Longitud de la media rápida, contada en muestras y no en velas: a cinco velas por muestra abarca cinco veces esa cantidad de velas de tiempo de mercado. |
| Slow EMA Length | 40 | Longitud de la media lenta, en muestras. Mantenla bien separada de la longitud rápida, o las dos líneas intercambiarán su posición por ruido y los cruces dejarán de significar algo. |
| Order Volume | 1 | Tamaño de cada orden de entrada, en unidades del instrumento. Los bloques de cierre lo ignoran y envían lo que la posición mantenga. |
| Take Profit, % | 1.5 | Distancia del take-profit, en porcentaje del precio de ejecución. |
| Stop Loss, % | 1 | Distancia del stop-loss, en porcentaje del precio de ejecución. |

## Detalles del diagrama

- El bloque N values se usa aquí como adelgazador y no como retardo. Sus dos entradas proceden del mismo flujo de velas, así que la cuenta atrás se reinicia en cuanto expira y el pulso sigue cayendo en cada quinta vela cerrada durante toda la ejecución.
- La Variable situada entre el pulso y las medias es lo que hace real el remuestreo: su entrada acepta todos los precios de cierre, su salida permanece en silencio hasta que llega el disparador, y así las medias reciben un valor por ventana y nunca ven las velas intermedias.
- Ambas medias leen la misma variable, por lo que avanzan al mismo paso y el bloque Crossing puede emparejar sus valores según van llegando. Solo informa de la muestra en la que cambió el orden de las dos líneas, y por eso es imposible entrar en las velas que quedan entre muestras.
- Las comprobaciones de la posición se escriben como no largo y no corto en lugar de como plano, de modo que un cruce que llega mientras el lado contrario sigue abierto alcanza igualmente el bloque de entrada; es el ajuste de solo apertura lo que mantiene el diagrama en una única posición, y son los bloques de cierre los que la liberan.
- Cada ejecución, tanto de entradas como de salidas, se une mediante un Combination y se envía a Position protection, de modo que el lado protector siempre ve la posición que la cuenta mantiene realmente y se retira en cuanto un cruce ha cerrado la operación.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
