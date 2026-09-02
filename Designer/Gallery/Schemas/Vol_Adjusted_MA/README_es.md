# Diagrama de la estrategia de media móvil ajustada por volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama envuelve una media móvil simple en un canal cuya semianchura equivale a varios rangos verdaderos medios: cuando el mercado se pone nervioso los bordes se separan y cuando se calma se juntan. Un cierre fuera de un borde se considera una ruptura real y la operación se devuelve en cuanto el precio regresa a la media.

![schema](schema.svg)

## Resumen de la estrategia

- SimpleMovingAverage traza la línea central y AverageTrueRange decide a qué distancia quedan los bordes, de modo que el canal se adapta a la volatilidad del momento.
- Dos bloques de fórmula construyen los bordes como SMA + multiplicador * ATR y SMA - multiplicador * ATR a partir de las mismas tres fuentes.
- Solo se entra desde posición plana y la única salida es que el cierre vuelva a cruzar la línea central; no hay stop ni objetivo, igual que en el original en C#.
- Dos diferencias con el original: la pausa de 500 barras tras cada operación no se reproduce, por lo que el diagrama opera más a menudo, y la vela de trabajo es de cinco minutos en lugar de uno, que es lo que trae el histórico incluido.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre está por encima del borde superior SMA + multiplicador * ATR y la posición está plana. El bloque de modificación compra a mercado el volumen compartido.
- **Entrada en corto**: El cierre está por debajo del borde inferior SMA - multiplicador * ATR y la posición está plana. El bloque de modificación vende a mercado el volumen compartido.
- **Salida**: El largo se devuelve en la primera vela que cierra por debajo de la SMA y el corto en la primera que cierra por encima; los bloques de cierre actúan solo cuando hay algo que cerrar.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de la media móvil simple que forma la línea central y el nivel de salida. |
| ATR Length | 14 | Periodo del rango verdadero medio que mide la volatilidad actual. |
| ATR multiplier | 2 | Cuántos ATR separan los bordes del canal de la línea central. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta ambos indicadores y un conversor que extrae el precio de cierre.
- Dos bloques de fórmula combinan la media, el rango y la constante multiplicadora en los bordes superior e inferior.
- Cuatro bloques de comparación forman las señales: dos contra los bordes del canal para las entradas y dos contra la línea central para las salidas.
- El bloque de posición, comparado con una constante cero, entra en cada Y lógica, así que ninguna orden aumenta una posición ya abierta.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
