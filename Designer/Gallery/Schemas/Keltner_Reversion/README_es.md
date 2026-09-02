# Diagrama de la estrategia de reversión al canal de Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un canal de Keltner es una media móvil con una envolvente de volatilidad: la anchura procede del Average True Range, de modo que las bandas respiran con el mercado en lugar de mantenerse a distancia fija. Este diagrama trata un cierre fuera del canal como un exceso, se posiciona en contra y devuelve la operación en la línea media.

![schema](schema.svg)

## Resumen de la estrategia

- El canal se monta a mano en vez de usar el indicador KeltnerChannels, porque ese bloque ata la media y el ATR a una única longitud, mientras que el original usa 20 para la EMA y 14 para el ATR.
- Dos bloques de fórmula construyen las bandas de forma literal: EMA más y menos el ATR por el multiplicador, con el multiplicador expuesto para ensanchar o estrechar el canal sin tocar el diagrama.
- La línea media es toda la regla de salida: la operación se devuelve en cuanto el precio vuelve al otro lado de la EMA, así que el objetivo se mueve con la media.
- El original trabaja con velas de un minuto y bloquea la operativa 500 barras tras cada operación, lo que en la práctica también sostiene la posición. El histórico incluido es de cinco minutos, así que el diagrama usa velas de cinco minutos; el bloqueo no se reproduce porque Designer no tiene contador de barras con estado, y por eso opera más a menudo y mantiene menos tiempo.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre está por debajo de la banda inferior, es decir, más de un ATR por el multiplicador por debajo de la EMA, y la posición está plana. La orden compra el volumen configurado.
- **Entrada en corto**: El cierre está por encima de la banda superior, es decir, más de un ATR por el multiplicador por encima de la EMA, y la posición está plana. La orden vende el volumen configurado.
- **Salida**: El largo se cierra cuando el cierre vuelve por encima de la EMA y el corto cuando vuelve por debajo. El original declara un multiplicador de stop que nunca utiliza, así que el diagrama tampoco lleva stop de pérdidas ni toma de beneficios.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| EMA Length | 20 | Periodo de la media móvil exponencial que forma la línea media. |
| ATR Length | 14 | Periodo del Average True Range que fija la anchura del canal. |
| ATR multiplier | 2 | Cuántos ATR separan las bandas de la línea media. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el conversor del precio de cierre y los dos bloques de indicador; el ATR necesita la vela completa, por lo que se conecta directamente a la fuente de velas.
- Cada banda es un bloque de fórmula con tres entradas: la EMA, el ATR y la constante compartida del multiplicador.
- Cuatro bloques de comparación contrastan el cierre con las dos bandas y con la línea media, y otros tres comparan la posición con cero.
- Cada Y lógica une una condición de precio con una de posición; los bloques de entrada llevan la condición de apertura y una constante de volumen compartida, y los de cierre la condición de cierre de posición.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
