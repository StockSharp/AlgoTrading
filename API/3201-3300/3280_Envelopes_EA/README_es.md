# Estrategia Envelopes EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia replica el asesor experto de MetaTrader 4 "EnvelopesEA". Aplica una envolvente de media móvil exponencial al flujo principal de velas y opera reversiones a la media. Cuando el mercado se aleja mucho fuera de la envolvente, se envía una orden de mercado contraria. Las posiciones se cierran tan pronto como el precio vuelve a entrar en la envolvente en la dirección opuesta. El experto original se probó en EUR/USD en 2019; la adaptación a StockSharp conserva la misma lógica y expone todas las entradas clave como parámetros optimizables.

## Lógica de trading
1. Calcular una media móvil exponencial (EMA) de longitud `EnvelopePeriod` sobre las velas seleccionadas.
2. Construir una envolvente superior e inferior expandiendo la EMA con `UpperDeviationPercent` y `LowerDeviationPercent`, respectivamente.
3. Aplicar un buffer adicional de entrada definido por `EntryOffsetPoints` (multiplicado por el paso de precio del instrumento) para evitar operaciones prematuras.
4. Cuando no hay posición abierta:
   - Entrar largo si el precio de cierre cae por debajo de la envolvente inferior menos el buffer de entrada.
   - Entrar corto si el precio de cierre sube por encima de la envolvente superior más el buffer de entrada.
5. Cuando existe una posición:
   - Cerrar posiciones largas cuando el precio de cierre vuelva a cruzar por encima de la envolvente superior.
   - Cerrar posiciones cortas cuando el precio de cierre vuelva a cruzar por debajo de la envolvente inferior.

La estrategia mantiene siempre como máximo una posición abierta y usa órdenes de mercado tanto para entradas como para salidas.

## Gestión monetaria
El volumen de la orden se especifica directamente mediante el parámetro `Volume` (lotes). No hay reglas automáticas de martingala ni piramidación, manteniendo el comportamiento idéntico a la última implementación MQ4, donde las funciones de escalado estaban desactivadas por defecto.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `Volume` | Volumen de orden en lotes. | 0.2 |
| `EnvelopePeriod` | Longitud de la EMA que forma la base de la envolvente. | 50 |
| `UpperDeviationPercent` | Desviación porcentual aplicada a la banda superior. | 0.5 |
| `LowerDeviationPercent` | Desviación porcentual aplicada a la banda inferior. | 0.5 |
| `EntryOffsetPoints` | Distancia adicional, en pasos de precio, que el precio debe recorrer más allá de la banda antes de entrar. | 100 |
| `CandleType` | Marco temporal usado para velas y cálculos de indicadores. | Velas de 30 minutos |

Todos los parámetros numéricos (excepto `CandleType`) están marcados como optimizables para ayudar a reproducir los flujos de optimización originales.

## Notas
- La envolvente usa una EMA en lugar de la SMA de versiones anteriores porque el script MQ4 evolucionó hacia una base exponencial en la iteración más reciente. Esto ofrece una reacción más rápida a los vaivenes del precio y mejora el timing de reversión a la media.
- El buffer de entrada se multiplica por el `PriceStep` del instrumento. Asegúrese de que los metadatos del valor contengan un tamaño de paso válido; de lo contrario, la estrategia usa un valor predeterminado conservador de `0.0001`.
- La visualización del gráfico incluye velas de precio, la envolvente EMA y las operaciones de la estrategia, lo que facilita validar el comportamiento de las señales frente al Expert Advisor original.
