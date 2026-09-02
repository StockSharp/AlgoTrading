# Diagrama de la estrategia de cruce del TRIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Aquí el TRIX no es un indicador de catálogo, sino una serie construida dentro del diagrama tal como la construye la estrategia original: una media exponencial triple y su variación relativa de una barra. El disparador es el cruce del cero por la serie rápida, la serie lenta debe moverse en el mismo sentido por encima de un umbral, y un objetivo y un stop porcentuales cierran la operación.

![schema](schema.svg)

## Resumen de la estrategia

- La materia prima son dos medias exponenciales triples del precio de cierre, de 9 y 21 barras; sendos bloques de valor anterior guardan cada una una vela atrás.
- El TRIX lento es un bloque de fórmula: la media menos su valor anterior, dividida por ese mismo valor anterior, que es la variación relativa por barra que el original calcula en código.
- El cruce del cero por el TRIX rápido se dibuja como el cruce de la media rápida con su propio valor anterior. Como una media de precios es positiva, el signo de la variación relativa coincide con el de la diferencia, así que el bloque de cruce es un sustituto exacto y ahorra la división.
- El umbral del TRIX lento es lo que mantiene al diagrama fuera del mercado lateral: el giro de la serie rápida solo se acepta mientras la lenta se mueve más de un 0,05 por ciento por barra en el mismo sentido.
- El original trabaja con velas de cuatro horas, objetivo de 1500 y stop de 500 en unidades absolutas de precio; el diagrama se reduce a cinco minutos para el histórico de muestra incluido y ambas distancias pasan a ser porcentajes del precio de entrada con la misma proporción de tres a uno.
- El indicador Trix incorporado se descarta a propósito: es una cadena de tres suavizados sucesivos con un factor de escala, así que sus valores y señales difieren de la media exponencial triple sobre la que está escrita la estrategia.

## Reglas de entrada y salida

- **Entrada en largo**: El TRIX rápido cruza el cero al alza, es decir, la media triple rápida gira hacia arriba tras caer, el TRIX lento está sobre el umbral y la posición no es larga. La orden compra un lote a mercado: abre un largo desde plano o cierra un corto de igual tamaño.
- **Entrada en corto**: El TRIX rápido cruza el cero a la baja, es decir, la media triple rápida gira hacia abajo tras subir, el TRIX lento está bajo el umbral negativo y la posición no es corta. La orden vende un lote a mercado: abre un corto desde plano o cierra un largo de igual tamaño.
- **Salida**: El bloque de protección cierra la operación en el objetivo o en el stop, ambos medidos en porcentaje del precio de entrada; por lo demás la posición se mantiene hasta la señal contraria, que la cierra porque todas las órdenes usan el mismo volumen.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Fast TEMA length | 9 | Periodo de la media exponencial triple rápida sobre la que se construye la serie disparadora. |
| Slow TEMA length | 21 | Periodo de la media exponencial triple lenta sobre la que se construye la serie de confirmación. |
| Volume | 1 | Volumen de la orden, en lotes; la misma constante alimenta ambos bloques de orden. |
| Take profit, % | 1.5 | Distancia del objetivo, en porcentaje del precio de entrada. |
| Stop loss, % | 0.5 | Distancia del stop, en porcentaje del precio de entrada. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- Un conversor extrae el precio de cierre de la vela y alimenta los dos bloques de indicador; ese mismo valor llega al bloque de protección como precio actual.
- Detrás de cada media hay un bloque de valor anterior: el par rápido entra en un bloque de cruce y el lento en un bloque de fórmula que divide la diferencia por el valor anterior.
- El bloque de cruce señala el giro al alza y un bloque NO lo invierte en el giro a la baja; dos comparaciones enfrentan la serie lenta a las constantes de umbral positiva y negativa.
- Cada Y lógica une el giro, la confirmación y una comprobación de posición, y dispara un bloque de modificación de posición; ambos bloques envían su operación al bloque de protección.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
