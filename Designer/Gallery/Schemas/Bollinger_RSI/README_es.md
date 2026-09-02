# Diagrama de la estrategia Bollinger Bands + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos herramientas clásicas responden aquí a dos preguntas distintas. Las Bollinger Bands indican cuánto se ha alejado el precio de su propia media, y el Relative Strength Index indica si el impulso que provocó ese alejamiento ya se agotó. Solo se opera cuando ambas coinciden, y la posición se abandona en cuanto el precio vuelve a la banda central.

![schema](schema.svg)

## Resumen de la estrategia

- Las Bollinger Bands y el Relative Strength Index se calculan sobre velas cerradas de un solo instrumento.
- Las bandas entregan tres números al diagrama a la vez: la banda superior, la inferior y la media móvil central.
- Una entrada exige un cierre fuera de la banda y una lectura del RSI en la zona extrema correspondiente; una sola condición nunca basta.
- La banda central es el objetivo: el regreso a ella cierra la posición, de modo que el diagrama no mantiene una operación que ya revirtió.

## Reglas de entrada y salida

- **Entrada en largo**: La vela cierra por debajo de la banda inferior de Bollinger, el RSI está por debajo del nivel de sobreventa y no hay posición. La orden compra un lote y abre un largo.
- **Entrada en corto**: La vela cierra por encima de la banda superior de Bollinger, el RSI está por encima del nivel de sobrecompra y no hay posición. La orden vende un lote y abre un corto.
- **Salida**: El largo se cierra cuando el cierre vuelve por encima de la banda central, y el corto cuando cae por debajo de ella. Ambas salidas usan bloques de modificación de posición en modo cierre, así que solo actúan si existe una posición del lado correspondiente; no hay stop de protección.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Bollinger Length | 20 | Periodo de suavizado de las Bollinger Bands. |
| Bollinger Width | 2 | Multiplicador de la desviación estándar que fija el ancho de las bandas. |
| RSI Length | 14 | Periodo de suavizado del Relative Strength Index. |
| RSI Oversold | 30 | Nivel por debajo del cual el RSI se considera sobrevendido. |
| RSI Overbought | 70 | Nivel por encima del cual el RSI se considera sobrecomprado. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta tres ramas: el bloque de Bollinger, el de RSI y un conversor que lee el precio de cierre.
- Tres bloques conversores separan el valor de Bollinger en banda superior, banda inferior y media móvil central.
- Seis bloques de comparación construyen las condiciones: el cierre frente a cada banda, el RSI frente a cada nivel y la posición frente a una constante cero.
- Cada Y lógica une una condición de banda, una de RSI y el control de posición, y dispara un bloque de modificación cuyo volumen viene de una constante compartida.
- La estrategia original hace una pausa de un número fijo de velas tras cada operación; no existe un bloque contador de velas, así que la pausa se omite y solo la banda central decide cuándo termina la operación.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
