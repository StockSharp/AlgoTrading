# Diagrama de la estrategia Bollinger %B de Larry Connors
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un diagrama de reversión a la media solo en largo, construido sobre Bollinger %B: la posición del cierre dentro de las bandas de Bollinger expresada como porcentaje de su anchura. La idea de Larry Connors es que una sola vela débil no demuestra nada, así que el diagrama espera a que %B se mantenga en la parte baja de la banda durante dos velas seguidas antes de comprar, y aguanta hasta que %B se recupera hacia la parte alta.

![schema](schema.svg)

## Resumen de la estrategia

- El indicador BollingerPercentB hace en un bloque lo que la estrategia original calcula a mano a partir de las bandas; su escala va de 0 a 100, por lo que los umbrales clásicos 0.35 y 0.8 se escriben 35 y 80.
- Un bloque de valor anterior guarda la lectura de la vela previa, y es lo que convierte una vela débil aislada en una condición de dos velas.
- La estrategia es solo larga: compra la debilidad y vende ese mismo largo, nunca abre un corto.
- La posición interviene en ambas decisiones, de modo que la entrada no se acumula y la salida no se dispara sin posición.

## Reglas de entrada y salida

- **Entrada en largo**: El %B de la vela anterior y el de la vela actual están ambos por debajo del umbral bajo, y la posición no es larga. La orden compra un lote.
- **Entrada en corto**: El diagrama nunca vende en corto. El bloque de venta solo sirve como salida de un largo abierto.
- **Salida**: El %B sube por encima del umbral alto mientras la posición es larga. La orden vende ese mismo lote y devuelve la posición a plano.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Bollinger Period | 20 | Periodo de las bandas de Bollinger sobre las que se calcula el %B. |
| Bollinger Deviation | 2 | Multiplicador de la desviación típica de las bandas de Bollinger. |
| Low %B | 35 | Umbral por debajo del cual %B cuenta como parte baja de la banda; debe cumplirse dos velas seguidas. |
| High %B | 80 | Umbral por encima del cual %B se considera recuperado, lo que cierra el largo. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque de indicador, cuyo valor va tanto a las comparaciones como al bloque de valor anterior.
- Dos comparaciones con la misma constante baja dan la condición de la vela actual y la de la anterior; una tercera contrasta %B con la constante alta para la salida.
- Otras dos comparaciones revisan la posición frente a cero: no larga para la entrada, larga para la salida.
- Los dos bloques Y lógicos disparan los bloques de modificación de posición, que toman su volumen de una única constante compartida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
