# Diagrama de la estrategia de sobrecompra y sobreventa del RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un diagrama clásico de reversión a la media: el índice de fuerza relativa mide cuánto se ha estirado el movimiento reciente y la estrategia se posiciona en contra cuando el índice llega a un extremo. El control de la posición evita acumular operaciones en el mismo sentido.

![schema](schema.svg)

## Resumen de la estrategia

- El índice de fuerza relativa se calcula sobre velas cerradas de un solo instrumento.
- Dos umbrales delimitan las zonas: por debajo del nivel de sobreventa el mercado se considera vendido en exceso; por encima del nivel de sobrecompra, comprado en exceso.
- La posición actual interviene en cada decisión, de modo que solo se entra cuando la orden no aumenta una posición ya abierta.

## Reglas de entrada y salida

- **Entrada en largo**: El RSI está en el nivel de sobreventa o por debajo y la posición no es larga. La orden compra un lote: abre un largo desde plano o cierra un corto existente.
- **Entrada en corto**: El RSI está en el nivel de sobrecompra o por encima y la posición no es corta. La orden vende un lote: abre un corto desde plano o cierra un largo existente.
- **Salida**: No hay bloque de salida propio: la señal contraria cierra la posición, porque todas las órdenes usan el mismo volumen.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| RSI Length | 14 | Periodo de suavizado del índice de fuerza relativa. |
| Oversold | 30 | Nivel en el que, o por debajo del cual, el índice se considera sobrevendido. |
| Overbought | 70 | Nivel en el que, o por encima del cual, el índice se considera sobrecomprado. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque de indicador que contiene el índice de fuerza relativa.
- Dos bloques de comparación contrastan el índice con las constantes de umbral; otros dos comparan la posición con cero.
- Cada Y lógica une una condición del índice con una de la posición y dispara un bloque de modificación de posición.
- Ambos bloques de modificación envían órdenes a mercado y toman el volumen de una constante compartida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
