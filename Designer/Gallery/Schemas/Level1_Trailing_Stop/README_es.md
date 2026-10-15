# Diagrama de estrategia de cruce de EMA con trailing stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La entrada de este diagrama es lo más sencillo de la paleta: un precio de cierre que cruza una media exponencial en velas de cuatro horas. De lo que trata realmente el ejemplo es del bloque que retira la posición después. Position protection se arma con la ejecución de entrada, recibe un precio en cada vela cerrada y, con el trailing activado, arrastra el stop por detrás de una operación que va a su favor, de modo que la salida es un nivel que avanza como un trinquete en lugar de una línea fija en la entrada.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de cuatro horas son el reloj del diagrama, y solo se publican las terminadas, así que cada decisión se toma sobre una barra ya completa.
- Un conversor extrae el precio de cierre de cada vela, y una media exponencial construida sobre la misma serie es el nivel con el que se compara el precio.
- Dos bloques de cruce vigilan ese par desde lados opuestos: uno toma el cierre como entrada superior y la media como inferior, y el otro los tiene al revés. Cada uno se pronuncia solo en la barra en la que las dos líneas realmente intercambian sus posiciones.
- La posición actual se ajusta al compás de las velas mediante una variable que la retiene en su entrada y la libera con el disparo de la vela; tres comparaciones contra cero convierten ese número retenido en los estados sin posición, largo y corto.
- Cuatro puertas lógicas AND emparejan los dos cruces con esos tres estados: un cruce al alza sin posición abre un largo, un cruce a la baja sin posición abre un corto, y cualquiera de los dos cruces contra una posición existente la cierra.
- Ambos bloques de entrada llevan la condición de posición abierta, de modo que una puerta que se dispara mientras ya hay una operación en curso no puede ni aumentarla ni darle la vuelta: el diagrama mantiene una sola posición a la vez.
- Cada ejecución propia llega a Position protection a través de un Combination, que es lo que mantiene honesta su idea de la posición: las entradas lo arman y las ejecuciones de cierre lo desarman.
- A su conector Price se le entrega el mismo precio de cierre que usan las señales, por lo que el nivel del trailing se recalcula una vez por vela cerrada; el panel del gráfico dibuja las velas, la media, las órdenes de entrada y de salida, el stop de protección y cada ejecución.

## Reglas de entrada y salida

- **Entrada en largo**: En una vela cerrada, el precio de cierre cruza por encima de la media exponencial mientras la posición retenida es cero. La puerta de largos deja pasar la señal y Position modify compra el volumen de la orden a mercado.
- **Entrada en corto**: En una vela cerrada, el precio de cierre cruza por debajo de la media exponencial mientras la posición retenida es cero. La puerta de cortos deja pasar la señal y Position modify vende el volumen de la orden a mercado.
- **Salida**: Dos cosas pueden terminar una operación, y normalmente lo hace la primera. Position protection coloca su stop a un 1.5% del precio de entrada y, con el trailing activado, lo lleva hacia arriba por detrás de un largo y hacia abajo por detrás de un corto cada vez que una vela cierra más adentro del beneficio; la operación se cierra con una orden a mercado en cuanto un cierre vuelve a atravesar ese nivel. La segunda salida es la propia señal: un cruce contra una posición abierta activa un tercer bloque Position modify configurado para cerrar, que deja plano lo que haya y no necesita volumen propio. Ninguna de las dos vías da la vuelta a la posición: la dirección contraria tiene que esperar al siguiente cruce, momento en el que el diagrama está sin posición y libre para tomarla.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 04:00:00 | Marco temporal de la serie de velas. Una vela cerrada es una decisión y un recálculo del nivel del trailing. |
| EMA Length | 15 | Número de velas de la media exponencial con la que se compara el precio de cierre. Una más larga cruza con menos frecuencia y mantiene una operación a través de más ruido. |
| Order Volume | 1 | Tamaño de la orden, en lotes. |
| Trailing Stop, % | 1.5 | Distancia del trailing stop, en porcentaje. Se mide desde el mejor precio alcanzado desde que se abrió la posición, no desde el precio de entrada. |

## Detalles del diagrama

- El nivel del trailing sigue al precio que se entrega al bloque, y aquí ese precio es un cierre. El stop se sitúa, por tanto, donde cerraron las velas y no donde llegaron sus mechas, de modo que un pico dentro de la barra ni arrastra el nivel ni saca la operación.
- Un trailing más fino está a un enlace de distancia: alimenta el conector Price desde un bloque de Level 1 que lea el precio de la última operación, o entrega el libro de órdenes al conector Market depth, y ese mismo stop se recalcula con cada cotización en lugar de una vez cada cuatro horas.
- Todas las ejecuciones propias entran en Position protection, incluidas las de cierre. Una ejecución de cierre devuelve a cero su posición acumulada y desarma el stop; sin ella, el bloque seguiría vigilando una posición que ya no existe y acabaría protegiéndola abriendo la contraria.
- La media publica únicamente valores formados y definitivos, por lo que las primeras barras, en las que todavía se está llenando, no pueden producir un cruce propio.
- Order Volume es un único número expuesto que comparten ambos bloques de entrada. El bloque de cierre no toma volumen alguno, porque una orden de cierre de posición se dimensiona a partir de lo que esté abierto.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
