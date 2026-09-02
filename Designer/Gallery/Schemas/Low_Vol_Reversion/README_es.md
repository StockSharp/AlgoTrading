# Diagrama de la estrategia de reversión en baja volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La reversión a la media funciona cuando el mercado no va a ninguna parte y sufre cuando hay tendencia, así que este diagrama solo opera mientras el mercado está tranquilo. La calma se define sin ninguna cifra absoluta: el Average True Range actual se compara con su propia media suavizada y solo se abre posición cuando queda por debajo de una fracción de esa media.

![schema](schema.svg)

## Resumen de la estrategia

- La volatilidad se mide respecto a sí misma: un AverageTrueRange alimenta una SmoothedMovingAverage y la relación entre ambos es todo el filtro de régimen, por lo que el diagrama se traslada a cualquier instrumento sin recalibrar.
- El suavizado reproduce exactamente la media recursiva del código original, porque SmoothedMovingAverage usa la misma fórmula: la media por la longitud menos uno, más el valor nuevo, dividido por la longitud.
- El valor justo es una SimpleMovingAverage corriente: un cierre por debajo se compra y uno por encima se vende, pero solo en régimen tranquilo y solo desde posición plana.
- El original trabaja con velas de un minuto y bloquea toda la estrategia durante 500 barras tras cada operación, salidas incluidas. El histórico incluido es de cinco minutos, así que el diagrama usa velas de cinco minutos; el bloqueo no se reproduce porque Designer no tiene contador de barras con estado, y por eso opera con más frecuencia que el original.

## Reglas de entrada y salida

- **Entrada en largo**: El Average True Range está por debajo del nivel de calma, el cierre queda bajo la media móvil y la posición está plana. La orden compra el volumen configurado.
- **Entrada en corto**: El Average True Range está por debajo del nivel de calma, el cierre queda sobre la media móvil y la posición está plana. La orden vende el volumen configurado.
- **Salida**: El largo se cierra cuando el cierre vuelve por encima de la media móvil y el corto cuando vuelve por debajo. Las salidas ignoran deliberadamente el filtro de volatilidad, de modo que la operación se devuelve incluso si el mercado ya se ha despertado. No hay stop de pérdidas ni toma de beneficios, igual que en la estrategia original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de la media móvil que hace de valor justo. |
| ATR Length | 14 | Periodo del Average True Range, la volatilidad actual. |
| ATR averaging length | 20 | Periodo con el que se suaviza el Average True Range para obtener su propia media. |
| Quiet threshold, % | 80 | Fracción de la volatilidad media, en porcentaje, por debajo de la cual el mercado se considera tranquilo. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el conversor del precio de cierre, la media móvil y el Average True Range; el rango pasa después a un segundo bloque de indicador que lo suaviza.
- Un bloque de fórmula convierte el rango suavizado y el porcentaje expuesto en el nivel de calma, y un bloque de comparación enfrenta el rango bruto a ese nivel.
- Dos bloques de comparación deciden a qué lado de la media está el cierre y se reutilizan: la condición que abre un largo también cierra un corto.
- Cada Y de entrada une tres condiciones —precio, volatilidad y posición plana— mientras que las Y de salida unen solo precio y posición, que es lo que hace que las salidas funcionen en cualquier régimen.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
