# Diagrama de la estrategia TTM Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Los mercados tranquilos no siguen tranquilos mucho tiempo. Este diagrama mide la anchura de las bandas de Bollinger como porcentaje de la banda media, considera el mercado comprimido mientras esa anchura se mantiene por debajo de su propia media móvil y opera en la primera vela en que las bandas vuelven a abrirse. El RSI decide hacia dónde.

![schema](schema.svg)

## Resumen de la estrategia

- Anchura = (banda superior - banda inferior) / banda media * 100, de modo que la lectura de compresión no depende del nivel de precio del instrumento.
- Una media móvil simple de esa anchura, multiplicada por el factor de compresión, marca la línea por debajo de la cual el mercado se considera comprimido.
- La operación se hace en la expansión, no en la compresión: la vela anterior debía estar dentro de la compresión y la anchura actual debe superarla.
- El RSI frente a su línea media da la dirección, y la banda de Bollinger contraria es donde se abandona la operación.

## Reglas de entrada y salida

- **Entrada en largo**: La anchura supera a la de la vela anterior, ese valor anterior estaba en el nivel de compresión o por debajo, el RSI está por encima de 50 y la posición está plana. La orden de compra abre un largo de un lote.
- **Entrada en corto**: La anchura supera a la de la vela anterior, ese valor anterior estaba en el nivel de compresión o por debajo, el RSI está por debajo de 50 y la posición está plana. La orden de venta abre un corto de un lote.
- **Salida**: El largo se cierra cuando el cierre cae por debajo de la banda inferior y el corto cuando sube por encima de la superior: la ruptura falló y se fue al otro lado. Ambas salidas trabajan en modo cierre de posición; la estrategia original tampoco lleva stop ni objetivo.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Bollinger Period | 20 | Periodo de suavizado de las bandas de Bollinger. |
| Bollinger Width | 2 | Anchura de las bandas de Bollinger, en desviaciones típicas. |
| RSI Length | 14 | Periodo del RSI que confirma la dirección. |
| Width Average Length | 20 | Longitud de la media móvil calculada sobre la propia anchura de las bandas. |
| Squeeze Factor | 0.9 | Fracción de esa media por debajo de la cual el mercado se considera comprimido; bájela para señales más escasas y exigentes. |
| RSI Midline | 50 | Nivel del RSI que separa la lectura alcista de la bajista. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:30:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de Bollinger se lee con tres conversores: banda superior, banda inferior y banda media; un cuarto conversor toma el cierre de la vela.
- Un bloque de fórmula convierte las tres bandas en la anchura porcentual, que alimenta a la vez un bloque de media móvil y un bloque de valor anterior, de forma que la anchura se compara con su propio pasado.
- Una segunda fórmula multiplica la anchura media por el factor de compresión y dos comparaciones producen las señales de compresión y de expansión.
- Cada entrada es una Y lógica de cuatro condiciones: expansión, compresión, dirección del RSI y posición plana; ambos bloques de entrada toman el volumen de la misma constante.
- La estrategia original mantiene además un mínimo móvil de la anchura, cuenta tres barras estrechas, filtra la dirección con una EMA(20) y pausa quince barras tras cada operación; el diagrama sustituye ese mínimo por la media móvil de la anchura y prescinde del contador, la EMA y la pausa, que ningún bloque puede expresar.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
