# Diagrama de la estrategia de reversión Morning Star
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Morning Star es el suelo clásico de tres velas: una vela bajista amplia, una vela pequeña indecisa y una vela alcista amplia que recupera más de la mitad de la primera. Su imagen especular, el Evening Star, marca un techo. Este diagrama reconoce ambas figuras con bloques de patrones de vela, abre posición solo cuando está plano y devuelve la operación en cuanto el precio cierra al lado equivocado de una media móvil simple.

![schema](schema.svg)

## Resumen de la estrategia

- Dos bloques de indicador de patrones de vela llevan expresiones propias de tres velas: la primera vela tiene cuerpo y apunta en sentido contrario a la entrada, el cuerpo intermedio es menor que la mitad del primero y la tercera cierra más allá del punto medio de la primera.
- Una media móvil simple del precio de cierre es la única referencia de salida; el diagrama no tiene stop loss ni take profit, igual que la estrategia original.
- El bloque de posición se compara con cero, de modo que solo se actúa sobre un patrón estando plano y nunca se añade a una operación abierta.
- La estrategia original congela además todas las señales durante varios cientos de barras tras cada ejecución; aquí no existe un bloque contador de barras, así que esa pausa se omite y se documenta.

## Reglas de entrada y salida

- **Entrada en largo**: El bloque Morning Star informa del patrón en la vela recién cerrada y la posición es cero. La orden compra un lote y abre un largo.
- **Entrada en corto**: El bloque Evening Star informa del patrón en la vela recién cerrada y la posición es cero. La orden vende un lote y abre un corto.
- **Salida**: Un largo se cierra con un bloque de modificación de posición en modo cierre en cuanto una vela cierra por debajo de la media móvil; un corto se cierra igual cuando una vela cierra por encima. No hay stop de protección porque la estrategia de origen tampoco lo tiene.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de la media móvil simple que cierra las operaciones. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas de todo el diagrama; el original usa velas de un minuto y aquí se ajusta al histórico de cinco minutos incluido en la galería. |

## Detalles del diagrama

- El bloque de velas alimenta cuatro ramas: los dos indicadores de patrón, la media móvil y un conversor que lee el precio de cierre.
- Cada bloque de patrón guarda una expresión de tres condiciones, así que la figura se reconoce sin una cadena de bloques de fórmula.
- Dos bloques de comparación sitúan el cierre a un lado u otro de la media y disparan directamente los dos bloques de cierre.
- Cada Y lógica une un patrón con el control de posición y dispara una entrada; ambas órdenes de entrada toman el volumen de una constante compartida, mientras que los bloques de cierre lo calculan a partir de la posición abierta.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
