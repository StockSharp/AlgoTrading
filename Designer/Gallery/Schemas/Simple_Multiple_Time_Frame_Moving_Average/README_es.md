# Diagrama de la estrategia Simple Multiple Time Frame Moving Average
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El nombre promete dos marcos temporales, pero la estrategia en C# de la que procede se suscribe a una sola serie de cuatro horas y calcula sobre ella dos ExponentialMovingAverage de longitudes distintas. Lo que realmente se opera es la coincidencia de sus pendientes: mientras la corta y la larga apuntan hacia arriba el diagrama está largo, mientras ambas apuntan hacia abajo está corto, y si discrepan la posición se deja quieta.

![schema](schema.svg)

## Resumen de la estrategia

- Dos bloques ExponentialMovingAverage, uno corto y otro largo, trabajan sobre la misma serie de velas; el diagrama conserva esa única suscripción en lugar de inventar un segundo marco temporal.
- La pendiente de cada media se lee comparando su valor actual con un bloque de valor anterior de una vela: una media que sube es sencillamente una media por encima de donde estaba.
- Todas las órdenes usan el volumen compartido fijo, así que la señal contraria solo deja la posición plana; abrir en el otro sentido exige una segunda señal igual en la vela siguiente, tal como hace el código original.
- La condición es un estado y no un evento: se revisa en cada vela cerrada, por eso se usan comparaciones y Y lógicas y no hace falta un bloque de cruce.

## Reglas de entrada y salida

- **Entrada en largo**: La ExponentialMovingAverage rápida está por encima de su propio valor una vela atrás, la lenta también, y la posición no es larga. El bloque de modificación compra a mercado el volumen compartido: abre un largo desde plano o cierra un corto existente.
- **Entrada en corto**: La ExponentialMovingAverage rápida está por debajo de su propio valor una vela atrás, la lenta también, y la posición no es corta. El bloque de modificación vende a mercado el volumen compartido: abre un corto desde plano o cierra un largo existente.
- **Salida**: No hay regla de salida propia: la posición la cierra la señal contraria, es decir, el momento en que ambas medias giran. La estrategia de origen no lleva stop loss, ni take profit, ni pausa entre operaciones, y este diagrama tampoco.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Fast EMA length | 5 | Periodo de la ExponentialMovingAverage rápida. |
| Slow EMA length | 20 | Periodo de la ExponentialMovingAverage lenta. |
| Volume | 1 | Volumen de la orden, en lotes; la misma constante alimenta los dos bloques de modificación. |
| Candles | 04:00:00 | Marco temporal de las velas de todo el diagrama; el original usa cuatro horas y se mantiene, lo que deja unas doscientas velas en el mes de histórico incluido. |

## Detalles del diagrama

- El bloque de velas alimenta los dos bloques de indicador, y cada indicador alimenta un bloque de valor anterior tipado como valor de indicador.
- Cuatro bloques de comparación convierten las dos medias y sus copias retrasadas en banderas de subida y de bajada.
- El bloque de posición, comparado dos veces con una constante cero, aporta el control que impide que una entrada aumente una posición ya abierta.
- Cada Y lógica une una condición de la media rápida, una de la lenta y una de la posición, y dispara un bloque de modificación que toma su tamaño de la constante de volumen compartida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
