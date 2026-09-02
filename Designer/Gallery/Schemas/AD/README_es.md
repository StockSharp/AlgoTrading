# Diagrama de la estrategia de tendencia de la línea de acumulación/distribución
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Aquí la dirección la marca el volumen. La línea de acumulación/distribución suma el lugar donde cerró cada vela dentro de su propio rango, ponderado por el volumen negociado: una línea que sube significa que los compradores absorbieron la oferta y una que baja, lo contrario. El diagrama compara la línea con su valor de una vela antes y se coloca del lado que el volumen respalda, siempre que la media móvil simple esté de acuerdo.

![schema](schema.svg)

## Resumen de la estrategia

- La línea de acumulación/distribución recibe la vela completa, porque necesita máximo, mínimo, cierre y volumen a la vez.
- Un bloque de valor anterior guarda la lectura de una vela antes, de modo que la pendiente de la línea se obtiene con una simple comparación y no con un segundo indicador.
- La media móvil simple actúa como filtro de permiso: puede entrar volumen, pero solo se compra si la vela además cierra por encima de la media.
- Ambas entradas llevan la condición de abrir posición y ambas salidas la de cerrarla, así se mantiene una sola posición y nunca se amplía.

## Reglas de entrada y salida

- **Entrada en largo**: La línea A/D está por encima de su valor anterior, la vela cierra por encima de la media móvil simple y la posición es plana. La orden compra el volumen compartido a mercado.
- **Entrada en corto**: La línea A/D está en su valor anterior o por debajo, la vela cierra por debajo de la media móvil simple y la posición es plana. La orden vende el volumen compartido a mercado.
- **Salida**: La pendiente por sí sola cierra la operación, sin condición de precio: la línea que retrocede cierra un largo y la que gira al alza cierra un corto. No hay stop loss ni take profit, igual que en la estrategia original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| MA Period | 20 | Periodo de la media móvil simple que decide hacia qué lado se permite entrar. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta a la vez tres consumidores: la línea A/D, la media móvil y el conversor que extrae el precio de cierre.
- La salida de la línea A/D va tanto al bloque de valor anterior como directamente a dos comparaciones, así que subida y bajada se leen del mismo par de números.
- Cada Y lógica une la pendiente de la línea, el lado de la media móvil y la comprobación de posición plana antes de disparar un bloque de entrada.
- Los dos bloques de salida cuelgan directamente de las comparaciones de pendiente y llevan la condición de cerrar posición, lo que hace que cada uno actúe en un solo sentido.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
