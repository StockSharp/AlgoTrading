# Diagrama de la estrategia del harami alcista
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un harami es una vela que cabe entera dentro de la anterior, señal de que el bando que acababa de empujar el mercado se ha quedado sin aire. El código original mide esa contención sobre los extremos y no sobre los cuerpos, así que lo que aquí se reconoce es una barra interior que además cambia de color: la vela previa fue en un sentido y la pequeña que queda dentro va en el contrario. Esa vuelta se toma desde plano y se entrega a una media móvil simple.

![schema](schema.svg)

## Resumen de la estrategia

- Dos bloques de patrón de velas llevan patrones propios escritos tal como los comprueba el código original: la vela anterior tiene un color, la actual el otro, y su máximo y su mínimo quedan dentro del rango previo.
- La media móvil simple del precio de cierre no filtra en absoluto la entrada; es solo el árbitro que decide cuándo termina la operación.
- Las entradas se permiten únicamente con la posición exactamente plana, y eso es lo que convierte al harami en un intento de giro y no en una forma de aumentar una operación en curso.
- Las salidas son bloques de modificación de posición en modo cierre, de modo que nunca abren nada por accidente.

## Reglas de entrada y salida

- **Entrada en largo**: El bloque del patrón alcista informa de una vela bajista seguida de una vela alcista más pequeña cuyo máximo está por debajo del máximo previo y cuyo mínimo está por encima del mínimo previo, y la posición está plana. La orden compra un lote y abre un largo.
- **Entrada en corto**: El bloque del patrón bajista informa de una vela alcista seguida de una vela bajista más pequeña contenida de la misma forma, y la posición está plana. La orden vende un lote y abre un corto.
- **Salida**: Un largo se cierra en cuanto una vela cierra por debajo de la media móvil y un corto en cuanto una vela cierra por encima, ambos mediante bloques de modificación de posición en modo cierre, tal como hace el original. El original además deja de operar durante quinientas velas después de cada orden; ningún bloque guarda un contador de barras entre velas, así que esa pausa se elimina y el diagrama opera cada patrón que encuentra estando plano. El original trabaja con velas de un minuto y el historial incluido es de cinco minutos, por lo que el diagrama se ejecuta sobre velas de cinco minutos.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de suavizado de la media móvil simple que cierra las operaciones. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta ambos bloques de patrón, la media móvil y un conversor que lee el precio de cierre.
- Dos bloques de comparación sitúan el cierre a un lado u otro de la media móvil; esas mismas dos señales accionan los dos bloques de cierre.
- Un bloque de comparación contrasta la posición con una constante cero y su salida la comparten las dos condiciones de entrada.
- Cada Y lógica une un patrón con la comprobación de posición plana y dispara un bloque de modificación de posición que toma su volumen de la constante compartida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
