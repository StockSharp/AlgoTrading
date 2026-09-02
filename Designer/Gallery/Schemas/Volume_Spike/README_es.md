# Diagrama de la estrategia de pico de volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una vela que mueve mucho más volumen que la anterior suele significar que alguien acaba de operar en tamaño. Este diagrama espera ese salto, deja que una media móvil simple diga si la multitud compra o vende y se suma mientras el volumen siga creciendo. En cuanto el volumen cae por debajo del de la vela anterior, la operación termina.

![schema](schema.svg)

## Resumen de la estrategia

- El volumen de la vela se compara con el de la vela anterior, no con una media de muchas velas, exactamente como hace el código original.
- La comparación está escrita como una multiplicación y no como una división, de modo que una vela sin volumen no puede romper el diagrama.
- Una media móvil simple de veinte velas sobre el precio de cierre elige el lado: por encima se compra el pico y por debajo se vende.
- Las entradas se hacen solo estando plano, y la salida no necesita ni la media ni el pico, únicamente un volumen que ha dejado de crecer.

## Reglas de entrada y salida

- **Entrada en largo**: El volumen de la vela es al menos el multiplicador por el volumen de la vela anterior, la vela cerró por encima de la media móvil y la posición está plana. La orden compra un lote a mercado.
- **Entrada en corto**: El volumen de la vela es al menos el multiplicador por el volumen de la vela anterior, la vela cerró por debajo de la media móvil y la posición está plana. La orden vende un lote a mercado.
- **Salida**: Ambos lados salen en la primera vela cuyo volumen es menor que el de la vela precedente, mediante bloques de modificación de posición en modo cierre. La estrategia original no tiene stop loss ni take profit, y este diagrama tampoco.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Spike Multiplier | 2 | Cuántas veces el volumen de la vela anterior debe alcanzar la vela actual para que el pico cuente. |
| SMA Length | 20 | Periodo de la media móvil simple que elige el lado de la entrada. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta un conversor de volumen, un conversor del precio de cierre y el bloque de la media móvil; un bloque de valor anterior desplazado una vela entrega el volumen de la vela previa.
- Una fórmula multiplica ese volumen previo por la constante del multiplicador y un bloque de comparación contrasta el volumen actual con el resultado.
- Cada Y lógica une el pico, el lado elegido por la media móvil y la comprobación de posición plana, y dispara un bloque de modificación de posición en modo de solo apertura.
- La comparación de volumen decreciente va directamente a los dos bloques de cierre, que están en modo cierre y por eso no hacen nada mientras el diagrama está plano. El original además pausa quinientas velas tras cada operación y trabaja con velas de un minuto; no existe bloque contador para esa pausa y el histórico incluido es más grueso que un minuto, así que el diagrama usa velas de cinco minutos y opera cada pico.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
