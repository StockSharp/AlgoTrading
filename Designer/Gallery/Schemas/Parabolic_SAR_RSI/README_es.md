# Diagrama de la estrategia Parabolic SAR + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Parabolic SAR decide de qué lado del mercado situarse y el índice de fuerza relativa solo puede vetar una entrada que se haría contra un movimiento ya agotado. La misma línea SAR que abre la operación también la cierra, de modo que la salida acompaña a la tendencia en lugar de quedarse en un precio fijo.

![schema](schema.svg)

## Resumen de la estrategia

- El Parabolic SAR se calcula sobre velas cerradas y se compara con el precio de cierre de cada vela: cierre por encima de la línea significa tendencia alcista, por debajo, bajista.
- El índice de fuerza relativa actúa como filtro laxo, igual que en el código original: un largo exige un RSI por debajo del nivel de sobrecompra y un corto un RSI por encima del nivel de sobreventa, así que solo se bloquean las entradas hechas directamente en un extremo.
- Las posiciones se abren únicamente desde plano y la única salida es el cambio de lado respecto al SAR: el diagrama no lleva stop fijo ni objetivo de beneficio.

## Reglas de entrada y salida

- **Entrada en largo**: La vela cierra por encima del Parabolic SAR, el RSI sigue por debajo del nivel de sobrecompra y la posición está plana. El bloque de modificación compra a mercado el volumen compartido.
- **Entrada en corto**: La vela cierra por debajo del Parabolic SAR, el RSI sigue por encima del nivel de sobreventa y la posición está plana. El bloque de modificación vende a mercado el volumen compartido.
- **Salida**: El largo se cierra en cuanto una vela cierra por debajo de la línea SAR y el corto en cuanto cierra por encima; ambos bloques de cierre operan con el tamaño que tenga la posición en ese momento.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| RSI Length | 14 | Periodo de suavizado del índice de fuerza relativa. |
| RSI Overbought | 70 | Nivel por debajo del cual debe estar el índice para permitir una entrada larga. |
| RSI Oversold | 30 | Nivel por encima del cual debe estar el índice para permitir una entrada corta. |
| SAR Acceleration | 0.02 | Factor de aceleración inicial del Parabolic SAR. |
| SAR Max acceleration | 0.2 | Límite superior del factor de aceleración del SAR. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta al Parabolic SAR, al índice de fuerza relativa y a un conversor que extrae el precio de cierre.
- Dos comparaciones sitúan el cierre respecto a la línea SAR, otras dos contrastan el índice con sus constantes y tres comparan la posición con cero.
- Cada Y lógica reúne una condición de precio, una de filtro y una de posición antes de disparar un bloque de modificación; los bloques de cierre usan el modo de cierre y no necesitan volumen.
- La pausa de 130 velas que la estrategia en C# respeta tras cada operación no tiene bloque equivalente en Designer, por lo que este diagrama vuelve a entrar antes y opera con más frecuencia.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
