# Diagrama de la estrategia de velas Heikin-Ashi consecutivas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Las velas Heikin-Ashi promedian el ruido, así que su color se mantiene mientras el movimiento dura de verdad. Este diagrama mide esa persistencia: siete cuerpos alcistas seguidos se toman como una tendencia establecida y se compran, siete bajistas seguidos se venden, y un stop loss porcentual limita lo que puede costar una serie falsa.

![schema](schema.svg)

## Resumen de la estrategia

- Un bloque de fórmula construye el cuerpo Heikin-Ashi como la media de apertura, máximo, mínimo y cierre menos el punto medio de la vela anterior: cuerpo positivo es vela alcista, negativo es bajista.
- La serie de velas del mismo color se mide sin contador: que el mínimo de los últimos siete cuerpos esté por encima de cero significa que las siete fueron alcistas, y que el máximo esté por debajo de cero, que las siete fueron bajistas.
- La orden se dimensiona como volumen más la posición absoluta, de modo que una sola orden gira un corto directamente a largo y al revés, igual que en el original en C#.
- La apertura Heikin-Ashi se define por su propio valor anterior, algo que un diagrama no puede realimentar a un bloque; en su lugar se usa el punto medio de la vela normal previa, así que las series halladas aquí son parecidas, pero no idénticas, a las que cuenta el código fuente.

## Reglas de entrada y salida

- **Entrada en largo**: El mínimo de los últimos siete cuerpos Heikin-Ashi está por encima de cero, es decir, las siete velas fueron alcistas, y la posición no es larga. La orden compra volumen más la posición absoluta: abre un largo desde plano o gira un corto.
- **Entrada en corto**: El máximo de los últimos siete cuerpos Heikin-Ashi está por debajo de cero, es decir, las siete velas fueron bajistas, y la posición no es corta. La orden vende volumen más la posición absoluta: abre un corto desde plano o gira un largo.
- **Salida**: No hay regla de salida propia, como en la estrategia de origen: la posición se gira con la serie contraria o la retira el bloque de protección, que coloca un stop loss a un porcentaje fijo del precio de ejecución. No hay objetivo ni stop dinámico.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Consecutive candles | 7 | Cuántas velas Heikin-Ashi del mismo color seguidas forman una señal; es el periodo tanto del bloque Lowest como del Highest. |
| Stop loss, % | 2 | Distancia del stop loss respecto al precio de entrada, en porcentaje. |
| Volume | 1 | Volumen base de la orden, en lotes; se le suma la posición absoluta para que el giro ocurra en una sola orden. |
| Candles | 00:30:00 | Marco temporal de las velas de todo el diagrama, la misma media hora que usa la estrategia original. |

## Detalles del diagrama

- El bloque de velas alimenta cuatro conversores de apertura, máximo, mínimo y cierre, y dos bloques de valor anterior entregan la vela previa a la fórmula.
- La salida de la fórmula entra en un bloque Lowest y otro Highest del mismo periodo, y dos comparaciones contra una constante cero los convierten en las dos condiciones de serie.
- El bloque de posición, comparado dos veces con cero, se une a cada condición mediante una Y lógica, así que ninguna orden aumenta una posición ya orientada correctamente.
- Ambos bloques de modificación toman su tamaño de una fórmula que suma la posición absoluta al volumen compartido, y sus ejecuciones alimentan el bloque de protección con el stop loss.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
