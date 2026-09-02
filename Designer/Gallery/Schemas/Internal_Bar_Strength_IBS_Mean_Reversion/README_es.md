# Diagrama de la estrategia de reversión a la media con Internal Bar Strength (IBS)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Internal Bar Strength hace una sola pregunta sobre una vela cerrada: ¿en qué punto de su propio rango cerró? Cero significa que cerró en el mínimo, uno que cerró en el máximo. Este diagrama solo vende, y solo contra la fuerza: una vela que rompe el máximo anterior y aun así termina pegada al techo de su rango se interpreta como un movimiento estirado que está a punto de devolver parte del recorrido.

![schema](schema.svg)

## Resumen de la estrategia

- El IBS no es aquí un bloque de indicador, sino una fórmula: (Cierre - Mínimo) dividido por el rango de la misma vela, de modo que toda la medida cabe en una expresión legible.
- Un bloque de valor anterior guarda el máximo de la vela previa, que es contra lo que se mide la condición de ruptura.
- La estrategia es corta por diseño: el bloque de compra existe únicamente para cerrar el corto y nunca abre un largo.
- No hay stop ni objetivo: la operación queda enteramente en manos del segundo umbral de IBS.

## Reglas de entrada y salida

- **Entrada en largo**: No hay entrada en largo. El diagrama solo vende, igual que la estrategia original.
- **Entrada en corto**: La vela cerró por encima del máximo de la vela anterior, su IBS está en el umbral superior o por encima y la posición no está ya corta. La orden vende un lote y abre un corto.
- **Salida**: El corto se recompra cuando el IBS de una vela cae al umbral inferior o por debajo, es decir cuando el cierre vuelve a la parte baja de su propio rango; la compra funciona en modo cierre, así que deja la posición en plano en lugar de girarla. El original no tiene stop loss ni take profit y aquí tampoco se añaden. Dos detalles se apartan del código. El original trabaja con velas de cuatro horas, de las que el histórico incluido de un mes solo daría unos cientos, así que el diagrama pasa a velas de cinco minutos. Y el original simplemente omite la vela cuyo máximo iguala al mínimo; aquí la fórmula divide por un rango acotado por debajo a un paso de precio, con lo que esa vela da un IBS de cero y no entra en ninguna de las condiciones. La SimpleMovingAverage que el original crea no se reproduce, porque su valor no interviene allí en ninguna decisión.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Upper IBS Threshold | 0.9 | Nivel de IBS en el que, o por encima del cual, se vende la vela de ruptura. |
| Lower IBS Threshold | 0.3 | Nivel de IBS en el que, o por debajo del cual, se recompra el corto. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama; el original usa velas de cuatro horas y este diagrama las de cinco minutos del histórico incluido. |

## Detalles del diagrama

- Tres conversores extraen del bloque de velas el cierre, el máximo y el mínimo de cada vela terminada.
- Un bloque de fórmula convierte esos tres números en Internal Bar Strength, con el rango acotado para que una vela plana no divida por cero.
- Un bloque de valor anterior retrasa el máximo una vela y una comparación mide el cierre contra él: esa es la mitad de ruptura de la entrada.
- El bloque de posición se compara dos veces con una constante cero: una guarda deja pasar la entrada solo si aún no hay corto, la otra permite la salida solo cuando el corto existe.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
