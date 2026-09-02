# Diagrama de cruce de media móvil con confirmación de volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un cruce de media móvil por sí solo reacciona a cualquier temblor del precio. Este diagrama acepta el cruce únicamente cuando llega acompañado de un salto real de actividad: la vela que cruza la SimpleMovingAverage debe negociar más que la anterior en un factor dado. El cruce contrario devuelve la posición y allí ya no se pide volumen.

![schema](schema.svg)

## Resumen de la estrategia

- Una SimpleMovingAverage de la vela marca la línea que el cierre debe cruzar, y un único bloque de cruce convierte las dos series en un solo evento de subida o bajada.
- El filtro de volumen compara la vela con su propia predecesora, no con una media: un bloque de valor anterior guarda el volumen de la vela previa, una fórmula lo multiplica por el factor y una comparación contrasta la vela nueva con el resultado.
- Solo se entra desde posición plana y con la confirmación de volumen; se sale únicamente con el cruce inverso, igual que en el original en C#.
- El original congela la operativa durante 150 barras tras cada orden; aquí no hay un bloque contador de barras, así que esa pausa se omite y el diagrama opera más a menudo.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre cruza la media móvil hacia arriba, el volumen de esa vela supera el de la anterior multiplicado por el factor, el volumen previo es mayor que cero y la posición está plana. El bloque de modificación compra a mercado el volumen compartido.
- **Entrada en corto**: El cierre cruza la media móvil hacia abajo con la misma confirmación de volumen y con la posición plana. El bloque de modificación vende a mercado el volumen compartido.
- **Salida**: El largo se cierra con el primer cruce bajista y el corto con el primer cruce alcista, sin condición de volumen; ambos bloques de cierre trabajan en modo cierre, así que actúan solo cuando hay algo que cerrar. Ni la estrategia original ni este diagrama llevan stop loss o take profit.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de la media móvil simple que el cierre debe cruzar. |
| Volume factor | 1.2 | Cuántas veces el volumen de la vela anterior debe superar la vela actual para aceptar la entrada. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta un conversor del volumen total, otro del precio de cierre y la media móvil.
- La cadena del volumen es valor anterior, fórmula y comparación; una segunda comparación contra cero evita que la primera vela pase el filtro de balde.
- Un solo bloque de cruce más un NO lógico cubren ambas direcciones: la salida propia es el cruce alcista y la negada, el bajista.
- Dos Y lógicas construyen las entradas con cruce, volumen y posición plana, y otras dos construyen las salidas con el cruce contrario y el signo de la posición.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
