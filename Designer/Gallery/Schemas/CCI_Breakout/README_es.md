# Diagrama de la estrategia de ruptura del CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Commodity Channel Index pasa la mayor parte del tiempo entre -100 y +100, así que salir de esa banda se interpreta como el inicio de un movimiento y no como un exceso. El diagrama compara el índice con su propio valor de la vela anterior, que es lo que convierte un nivel en una ruptura, y siempre está en mercado: cada señal invierte la posición en lugar de limitarse a cerrarla.

![schema](schema.svg)

## Resumen de la estrategia

- Un bloque de indicador calcula el Commodity Channel Index y un bloque de valor anterior guarda la lectura de la vela previa, de modo que el par describe un cruce del nivel y no solo una posición por encima de él.
- Los dos niveles son constantes normales, así que la banda de ruptura se puede ampliar, estrechar y optimizar como cualquier otro parámetro.
- El volumen de la orden es el volumen base más el valor absoluto de la posición actual, con lo que una sola orden a mercado cierra la posición contraria y abre la nueva.
- La estrategia original salta dos velas tras cada señal; ese contador no tiene equivalente en bloques y se omite, por lo que este diagrama puede girar una o dos velas antes que el código fuente.
- El original trabaja con velas de una hora; el diagrama se ha reducido a velas de cinco minutos para ajustarse al histórico de muestra incluido.

## Reglas de entrada y salida

- **Entrada en largo**: El CCI cerró la vela anterior en el nivel superior o por debajo y ahora está por encima, y la posición no es ya larga. La orden compra el volumen base más el corto abierto, invirtiendo la posición a largo.
- **Entrada en corto**: El CCI cerró la vela anterior en el nivel inferior o por encima y ahora está por debajo, y la posición no es ya corta. La orden vende el volumen base más el largo abierto, invirtiendo la posición a corto.
- **Salida**: No hay salida propia: la estrategia permanece en mercado y la ruptura contraria cierra la operación en curso y abre la nueva. El código original tampoco tiene stop loss ni take profit.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| CCI Length | 20 | Periodo de suavizado del Commodity Channel Index. |
| Upper level | 100 | Nivel que el índice debe cruzar al alza para una ruptura larga. |
| Lower level | -100 | Nivel que el índice debe cruzar a la baja para una ruptura corta. |
| Volume | 1 | Volumen base de la orden, en lotes; la inversión le suma la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el Commodity Channel Index, cuya salida va tanto a los bloques de comparación como al bloque de valor anterior.
- Dos bloques de comparación por lado contrastan la lectura actual y la anterior con la misma constante de nivel, lo que reproduce con exactitud la condición de ruptura del código fuente.
- Cada Y lógica une la lectura actual, la anterior y una comprobación de posición antes de disparar un bloque de modificación de posición.
- Un bloque de fórmula suma el volumen base al valor absoluto de la posición y alimenta ambos bloques de orden, de forma que una sola orden a mercado ejecuta toda la inversión.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
