# Diagrama de estrategia de entradas alternas con tamaño aleatorio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Todo diagrama de la galería decide cuánto operar; este se niega a hacerlo. El bloque Random sortea un tamaño nuevo entre medio lote y dos en cada vela y lo entrega directamente al conector de volumen de los dos bloques de entrada, de modo que no hay dos posiciones del mismo tamaño. La dirección, en cambio, no tiene nada de aleatoria: sale de dónde queda la última operación ejecutada respecto a la apertura de la vela en curso.

![schema](schema.svg)

## Resumen de la estrategia

- El flujo de ticks es la señal. Un conversor lee el precio de la última operación ejecutada y una variable lo retiene hasta que la vela cierra, que es lo que pone ese precio y la apertura de la vela en el mismo reloj.
- Una comparación pregunta si ese precio está por encima de la apertura de la vela. La misma respuesta invertida por un NOT lógico es el lado corto, así que una sola comparación sirve para las dos direcciones.
- El bloque Random se dispara con la vela y entrega su número al conector de volumen del bloque de compra y del de venta. Nada más en el diagrama lo lee.
- Las entradas se toman únicamente sin posición abierta, y los dos bloques llevan la condición sobre la posición abierta, de forma que una señal no puede sumar a una posición que ya está abierta.
- Un contador de velas desde la última ejecución mantiene doce velas entre entradas. Sin él, la salida protectora y la siguiente entrada se persiguen en velas consecutivas.
- Position protection es la única salida. Recoge las ejecuciones de entrada a través de un Combination, las valora contra el cierre de la vela y cierra con un objetivo del 0.4% o un stop dinámico del 0.5%.
- El stop es dinámico, así que una posición que avanza en la dirección correcta solo devuelve el último tramo del movimiento.
- El panel de gráfico dibuja las velas, los dos flujos de órdenes y todas las ejecuciones, incluidas las protectoras.

## Reglas de entrada y salida

- **Entrada en largo**: La última operación ejecutada está por encima de la apertura de la vela en curso, no hay posición abierta y han pasado doce velas desde la última ejecución. Position modify compra a mercado con el volumen que el bloque Random sorteó para esta vela.
- **Entrada en corto**: La última operación ejecutada está en la apertura de la vela en curso o por debajo de ella, con las mismas condiciones de posición plana y de espera. Position modify vende a mercado con el mismo volumen sorteado al azar.
- **Salida**: No hay señal de salida. Position protection se hace cargo de la posición desde la primera ejecución y la cierra con un take-profit del 0.4% o un stop dinámico del 0.5% medidos desde el precio de entrada. Cualquier ejecución, incluidas las protectoras, reinicia la espera de doce velas antes de la siguiente entrada.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de las velas con las que trabaja todo el diagrama. |
| Cooldown Bars | 12 | Cuántas velas cerradas deben pasar tras una ejecución antes de permitir la siguiente entrada. |
| Min Volume | 0.5 | Tamaño mínimo que puede sortear el bloque Random. |
| Max Volume | 2 | Tamaño máximo que puede sortear el bloque Random. |
| Take Profit, % | 0.4 | Distancia del take-profit, en porcentaje del precio de entrada. |
| Stop Loss, % | 0.5 | Distancia del stop dinámico (trailing stop), en porcentaje del precio de entrada. |

## Detalles del diagrama

- El bloque de velas alimenta a ocho consumidores: los dos conversores, la variable del último precio, el bloque Random, el contador de espera con sus dos variables y el panel de gráfico.
- La variable del último precio es lo único que se interpone entre un flujo de ticks que se dispara miles de veces al día y una condición pensada para responderse una vez por vela.
- Los dos bloques de entrada comparten una única salida del Random, así que el largo y el corto se dimensionan con el mismo sorteo; el número cambia en la vela siguiente, no entre un bloque y otro.
- El contador de espera se reinicia desde el bloque de ejecuciones de la estrategia, y por eso una salida protectora también pone en marcha la espera, no solo una entrada.
- El tamaño se sortea con dos decimales, lo que encaja con un instrumento cotizado en fracciones de unidad; en un instrumento que se opera por lotes enteros, el rango se fija en números enteros.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
