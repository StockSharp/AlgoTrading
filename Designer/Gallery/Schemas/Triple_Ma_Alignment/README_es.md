# Diagrama de la estrategia de alineación de tres EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Tres bloques ExponentialMovingAverage de longitudes muy distintas se calculan sobre las mismas velas y el diagrama interpreta su orden como la tendencia. Si la corta está por encima de la media y la media por encima de la larga, el mercado sube; si se ordenan al revés, baja. La estrategia está siempre en el mercado y cambia de lado con una sola orden.

![schema](schema.svg)

## Resumen de la estrategia

- Solo se usa el precio: ni oscilador ni filtro de volatilidad, únicamente la posición relativa de tres medias exponenciales.
- El estado alcista es corta sobre media y media sobre larga; el bajista es corta igual o por debajo de la media y media igual o por debajo de la larga. En medio, con las medias enredadas, no ocurre nada.
- La posición actual condiciona cada entrada, de modo que una alineación que dura cientos de velas genera exactamente una orden.
- No hay salida propia: el tamaño de la orden es el volumen más la posición en valor absoluto, así que una sola orden cierra el lado viejo y abre el nuevo.

## Reglas de entrada y salida

- **Entrada en largo**: La ExponentialMovingAverage corta está por encima de la media, la media por encima de la larga y la posición todavía no es larga. La orden compra el volumen más la posición absoluta: abre un largo desde plano o gira un corto a largo.
- **Entrada en corto**: La ExponentialMovingAverage corta está igual o por debajo de la media, la media igual o por debajo de la larga y la posición todavía no es corta. La orden vende el volumen más la posición absoluta: abre un corto desde plano o gira un largo a corto.
- **Salida**: No existe un bloque de salida propio. Solo se abandona la posición cuando aparece la alineación contraria, y el tamaño de giro hace que el diagrama no quede fuera del mercado ni una vela.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Short EMA period | 100 | Longitud de la ExponentialMovingAverage más rápida. |
| Middle EMA period | 250 | Longitud de la ExponentialMovingAverage intermedia. |
| Long EMA period | 500 | Longitud de la ExponentialMovingAverage más lenta. |
| Volume | 1 | Volumen base de la orden, en lotes; al girar se le suma la posición absoluta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- Un único bloque de velas alimenta los tres bloques de indicador, de forma que las medias siempre se calculan sobre las mismas velas cerradas.
- Cuatro bloques de comparación construyen los dos estados: dos «mayor que» estrictos para la pila alcista y dos «menor o igual» para la bajista, que es justo la negación empleada en el código original.
- Cada Y lógica une las dos comparaciones de medias con la posición contrastada frente a una constante cero y dispara un bloque de modificación de posición.
- Un bloque de fórmula suma la posición absoluta a la constante de volumen y alimenta ambos bloques de órdenes: eso es lo que convierte una entrada en un giro.
- Simplificaciones deliberadas: el original usa velas de un minuto y este diagrama velas de cinco, así que las mismas longitudes cubren cinco veces más tiempo. El original además recuerda si la alineación ya existía en la vela anterior; esa marca se elimina, porque el control de la posición bloquea igual de bien una entrada repetida. El stop del 2% declarado nunca se aplica en el código, así que no se dibuja bloque de protección.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
