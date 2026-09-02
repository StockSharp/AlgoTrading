# Diagrama de la estrategia de giro tras una sesión perdedora
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La idea es el giro después de una mala sesión: una sesión que termina por debajo de donde abrió suele dejarle un rebote a la siguiente, así que el diagrama espera a que el mercado se recupere por encima de su media móvil y compra esa recuperación, y hace lo contrario tras una sesión que cerró al alza. A pesar del nombre, la estrategia original no contiene ningún filtro por día de la semana, y este diagrama tampoco.

![schema](schema.svg)

## Resumen de la estrategia

- Dos series de velas trabajan a la vez: la serie de sesión decide hacia dónde inclinarse y la serie de negociación, más rápida, elige el momento de entrar.
- El veredicto de la sesión es una única comparación del cierre de la vela de sesión con su propia apertura, así que no hay que recordar ningún estado entre velas.
- La media móvil simple sobre la serie de negociación es la confirmación: tras una sesión perdedora solo se compra cuando el precio ya ha vuelto por encima de la media.
- Como el veredicto llega una vez por vela de sesión, la Y lógica solo puede dispararse una vez por sesión, que es exactamente la regla de una entrada por sesión del original.

## Reglas de entrada y salida

- **Entrada en largo**: La última sesión cerró por debajo de su apertura, la vela de negociación cierra por encima de la media móvil simple y la posición es plana. La orden compra el volumen compartido a mercado.
- **Entrada en corto**: La última sesión cerró por encima de su apertura, la vela de negociación cierra por debajo de la media móvil simple y la posición es plana. La orden vende el volumen compartido a mercado.
- **Salida**: Se sale por el lado de la media, no por un objetivo: un cierre de vuelta por debajo de la media cierra un largo y un cierre de vuelta por encima cierra un corto. No hay stop loss ni take profit, igual que en la estrategia original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| MA Period | 20 | Periodo de la media móvil simple que confirma el giro en la serie de negociación. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Trading candles | 00:05:00 | Marco temporal con el que se cronometran las entradas y las salidas. |

## Detalles del diagrama

- El bloque de velas de sesión alimenta dos conversores, uno para la apertura y otro para el cierre, y las dos comparaciones entre ellos dan las señales de sesión bajista y alcista.
- El bloque de velas de negociación alimenta la media móvil y un conversor del precio de cierre; dos comparaciones sitúan ese cierre a un lado u otro de la media.
- Cada Y lógica une una señal de sesión, una señal de lado de la media y la comprobación de posición plana antes de disparar un bloque de entrada con condición de abrir posición.
- Los bloques de salida cuelgan directamente de las dos comparaciones con la media y llevan la condición de cerrar posición, así que cada uno liquida solo su propio lado.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
