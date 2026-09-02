# Diagrama de la estrategia combinada EMA + MACD + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Tres comprobaciones independientes deben coincidir antes de que este diagrama opere. La posición relativa de la EMA 50 y la EMA 200 dice qué lado está permitido, el cruce de la línea MACD con su señal dice cuándo, y el RSI tiene que estar en una banda intermedia: con impulso ya visible pero sin que el movimiento esté agotado. Cada señal aceptada invierte la posición con una sola orden a mercado.

![schema](schema.svg)

## Resumen de la estrategia

- El filtro de tendencia es una comparación de niveles entre dos medias exponenciales: no se compra mientras la EMA 50 está por debajo de la EMA 200 ni se vende mientras está por encima.
- La entrada es un suceso, no un estado: solo la vela en la que la línea MACD cruza su señal puede abrir una operación, así que el diagrama no dispara sin parar mientras dura la tendencia.
- El corredor del RSI es lo que da prudencia a la combinación. Un largo necesita el RSI por encima del nivel de compra y todavía por debajo del límite superior; un corto, por debajo del nivel de venta y todavía por encima del límite inferior.
- El original trabaja con velas de treinta minutos; el diagrama se ha reducido a velas de cinco minutos para ajustarse al histórico de muestra incluido. Su pausa de diez barras tras cada operación no tiene equivalente en bloques y se omite, por lo que las reentradas son más frecuentes que en el código.

## Reglas de entrada y salida

- **Entrada en largo**: La EMA 50 está por encima de la EMA 200, la línea MACD cruza al alza su señal, el RSI está sobre el nivel de compra y aún bajo el límite superior, y la posición no es ya larga. La orden compra el volumen base más el corto abierto, de modo que una sola orden a mercado invierte el corto en largo.
- **Entrada en corto**: La EMA 50 está por debajo de la EMA 200, la línea MACD cruza a la baja su señal, el RSI está bajo el nivel de venta y aún sobre el límite inferior, y la posición no es ya corta. La orden vende el volumen base más el largo abierto, invirtiendo la posición a corto con una sola orden.
- **Salida**: No hay bloque de salida ni protección, igual que en el original: la posición se mantiene hasta que aparece la señal espejo, y esa misma orden cierra la operación antigua y abre la nueva.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Fast EMA length | 50 | Periodo de la media exponencial rápida que refleja la tendencia corta. |
| Slow EMA length | 200 | Periodo de la media exponencial lenta con la que se compara la rápida. |
| MACD fast length | 12 | Periodo de la EMA rápida dentro del MACD. |
| MACD slow length | 26 | Periodo de la EMA lenta dentro del MACD. |
| MACD signal length | 9 | Periodo de la EMA que suaviza el MACD hasta la línea de señal. |
| RSI length | 14 | Periodo de suavizado del índice de fuerza relativa. |
| RSI buy level | 40 | El RSI debe superar este nivel para aceptar un largo. |
| RSI sell level | 60 | El RSI debe estar por debajo de este nivel para aceptar un corto. |
| RSI upper bound | 70 | Límite superior del corredor del RSI; por encima, el largo se considera tardío. |
| RSI lower bound | 30 | Límite inferior del corredor del RSI; por debajo, el corto se considera tardío. |
| Volume | 1 | Volumen base de la orden, en lotes; la inversión le suma la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- Un bloque de velas alimenta cuatro bloques de indicador: las dos medias exponenciales, el MACD con su señal y el índice de fuerza relativa.
- Dos conversores separan el valor del MACD en las líneas Macd y Signal; un bloque de cruce convierte ese par en el disparador alcista y un bloque NO lo invierte en el bajista.
- Ocho bloques de comparación forman los filtros: un par para las medias, cuatro para el corredor del RSI y dos para la posición frente a cero.
- Cada Y lógica une cinco condiciones antes de disparar un bloque de modificación de posición, y un bloque de fórmula suma el volumen base al valor absoluto de la posición para que una sola orden a mercado realice toda la inversión.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
