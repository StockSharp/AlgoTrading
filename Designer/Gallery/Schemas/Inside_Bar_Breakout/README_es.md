# Diagrama de la estrategia de ruptura del inside bar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un inside bar es una vela cuyo rango completo cabe dentro del rango de la vela anterior: compradores y vendedores han dejado de empujar y el mercado queda comprimido. El diagrama espera a que la vela inmediatamente siguiente salga de ese rango y toma la ruptura en el sentido de la salida; a partir de ahí una media móvil simple lleva la operación y decide cuándo se ha agotado el movimiento.

![schema](schema.svg)

## Resumen de la estrategia

- Dos bloques de patrón de velas llevan cada uno una fórmula de tres velas: una primera vela sin restricciones, un inside bar estrictamente contenido en ella y una vela de ruptura.
- La fórmula larga exige que la vela de ruptura tenga un máximo por encima del máximo del inside bar; la corta, un mínimo por debajo de su mínimo.
- La media móvil simple del precio de cierre es el único indicador: no interviene en la entrada y se usa solo como línea de salida.
- El control de la posición garantiza que la ruptura se opere únicamente estando plano, así un patrón produce una sola operación.

## Reglas de entrada y salida

- **Entrada en largo**: El bloque de patrón informa de un inside bar cuyo máximo acaba de ser superado por la vela siguiente y la posición está plana. La orden compra un lote y abre un largo.
- **Entrada en corto**: El bloque de patrón informa de un inside bar cuyo mínimo acaba de ser perforado por la vela siguiente y la posición está plana. La orden vende un lote y abre un corto.
- **Salida**: El largo se cierra cuando una vela cierra por debajo de la media móvil y el corto cuando cierra por encima, ambos mediante bloques de modificación de posición en modo cierre, igual que en la estrategia original. Lo que el diagrama no puede reproducir es la espera indefinida del código: allí se recuerdan los extremos del inside bar y la ruptura se acepta muchas velas después, mientras que aquí el bloque de patrón solo ve una ventana de longitud fija, de modo que la ruptura debe llegar en la vela inmediatamente posterior. Es el caso habitual del patrón, pero las rupturas tardías se pierden. La pausa de varios cientos de barras entre operaciones tampoco tiene bloque propio y se ha omitido.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de suavizado de la media móvil simple que cierra las operaciones. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta los dos bloques de patrón, la media móvil y un convertidor que extrae el precio de cierre de la vela.
- Cada bloque de patrón contiene tres fórmulas, una por vela del patrón, y devuelve verdadero solo en la vela que completa la ruptura.
- El bloque de posición se compara con una constante cero y cada Y lógica une esa protección con una de las dos señales de ruptura.
- Ambos bloques de entrada envían órdenes a mercado y toman el volumen de una constante compartida; los dos bloques de salida se disparan directamente desde las comparaciones con la media y solo actúan cuando hay algo que cerrar.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
