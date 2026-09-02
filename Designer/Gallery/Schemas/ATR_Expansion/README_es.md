# Diagrama de la estrategia de expansión del ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Aquí la señal es la propia volatilidad. El Average True Range se compara con su valor de la vela anterior: cuando crece al menos en la proporción indicada, algo se ha puesto en marcha y el diagrama se suma al movimiento en el sentido que marca la media móvil simple. Cuando el rango se encoge en esa misma proporción, el movimiento se da por terminado y la posición se cierra.

![schema](schema.svg)

## Resumen de la estrategia

- El Average True Range mide la amplitud de las últimas velas y un bloque de valor anterior guarda la lectura de una vela antes para poder compararlas.
- La expansión es un ATR igual o superior al ATR anterior multiplicado por la proporción; la contracción es la imagen especular: el ATR anterior por encima del ATR actual multiplicado por esa misma proporción.
- La media móvil simple solo decide el lado: con el cierre por encima, la expansión es una compra; por debajo, una venta.
- Ambos bloques de entrada llevan la condición de apertura y ambos de salida la de cierre, así que el diagrama mantiene una sola posición y nunca la aumenta.

## Reglas de entrada y salida

- **Entrada en largo**: La volatilidad se expande, la vela cierra por encima de la media móvil simple y la posición está plana. La orden compra a mercado el volumen compartido.
- **Entrada en corto**: La volatilidad se expande, la vela cierra por debajo de la media móvil simple y la posición está plana. La orden vende a mercado el volumen compartido.
- **Salida**: La volatilidad se contrae, es decir, el ATR multiplicado por la proporción cae por debajo del ATR anterior. El lado que esté abierto se cierra a mercado con el bloque correspondiente; no hay stop de pérdidas ni toma de beneficios, igual que en la estrategia original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| ATR Period | 14 | Periodo de suavizado del Average True Range que mide la volatilidad. |
| MA Period | 20 | Periodo de la media móvil simple que decide la dirección de la entrada. |
| Expansion ratio | 1.05 | Cuánto mayor debe ser el nuevo ATR respecto al anterior para considerarse expansión; su inverso es el umbral de contracción que cierra la posición. |
| Volume | 1 | Volumen de la orden, en lotes, compartido por los dos bloques de entrada. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el ATR, la media móvil y un conversor que lee el precio de cierre.
- Un bloque de valor anterior guarda el ATR de la vela precedente y dos bloques de fórmula le aplican la proporción: uno construye el nivel de expansión y el otro el de contracción.
- Dos bloques de comparación convierten esos niveles en indicadores de expansión y contracción, y otros dos sitúan el cierre frente a la media móvil.
- Cada Y lógica reúne volatilidad, dirección y la comparación de la posición con cero, y dispara uno de los dos bloques de entrada; el indicador de contracción por sí solo dispara los dos bloques de cierre, cuya dirección decide qué lado pueden cerrar.
- Dos cosas del original en C# no se trasladan: la pausa de quinientas velas tras cada operación, que no tiene bloque equivalente, y las velas de un minuto, sustituidas por las de cinco minutos del histórico que acompaña a la galería.
- También se omite el parámetro Lookback del original, porque el código nunca lo lee.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
