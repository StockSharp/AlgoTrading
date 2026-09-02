# Diagrama de la estrategia de expansión del ancho de las bandas de Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La señal es la distancia entre las dos bandas de Bollinger, no que el precio las toque. Un bloque de fórmula resta la banda inferior de la superior y el resultado se retiene una vela para poder comparar las dos lecturas. En cuanto las bandas empiezan a abrirse el diagrama toma posición, y el lado lo decide únicamente dónde cerró la vela respecto a la banda media.

![schema](schema.svg)

## Resumen de la estrategia

- Las bandas de Bollinger entregan tres líneas a la vez; tres bloques conversores extraen la banda superior, la inferior y la media del mismo valor del indicador.
- El ancho lo calcula un bloque de fórmula y lo guarda un bloque de valor anterior, con lo que la expansión se reduce a comparar dos números.
- La dirección no es una prueba de ruptura: cualquier expansión abre una operación y la banda media solo dice si es larga o corta. Así se ramifica exactamente la estrategia original.
- En cuanto el ancho deja de crecer, se disparan los dos bloques de cierre y el lado abierto queda plano.

## Reglas de entrada y salida

- **Entrada en largo**: El ancho es mayor que en la vela anterior, la vela cerró por encima de la banda media y la posición es plana. La orden compra el volumen compartido a mercado.
- **Entrada en corto**: El ancho es mayor que en la vela anterior, la vela cerró en la banda media o por debajo y la posición es plana. La orden vende el volumen compartido a mercado.
- **Salida**: El ancho deja de crecer, es decir queda igual o por debajo del ancho de la vela anterior. Se disparan ambos bloques de cierre y el que corresponde al lado abierto lo liquida a mercado. La estrategia original no tiene stop loss ni take profit, y este diagrama tampoco.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Bollinger Period | 20 | Periodo de suavizado de las bandas de Bollinger, que fija la rapidez de reacción del ancho. |
| Bollinger Width | 2 | Multiplicador de la desviación estándar de las bandas; un valor mayor las separa más entre sí. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el indicador de bandas de Bollinger y, aparte, un conversor que lee el precio de cierre.
- El bloque de fórmula toma la banda superior como a y la inferior como b y devuelve su diferencia como ancho de banda.
- El ancho va tanto al bloque de valor anterior como directamente a dos comparaciones, así que la expansión y su ausencia se leen del mismo par de números.
- Cada Y lógica une expansión, lado de la banda media y comprobación de posición plana; los bloques de salida cuelgan directamente de la comparación de contracción.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
