# Estrategia espejo MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia MA Mirror es una conversión del experto MetaTrader *MA_MirrorEA*. El sistema compara dos medias móviles simples.
calculado en el mismo período pero utilizando diferentes fuentes de precios: cierres de velas versus aperturas de velas. Cuando la media móvil de
los precios de cierre se mantienen por encima de la media móvil de los precios de apertura; la estrategia quiere ser larga; cuando cae por debajo de la abertura
En promedio, la estrategia quiere ser corta. Un parámetro de cambio configurable permite leer los promedios móviles de velas más antiguas.
para que el puerto StockSharp pueda reproducir el desplazamiento visual aplicado en el indicador MetaTrader original.

La implementación StockSharp mantiene el comportamiento "espejo" original: sólo puede existir una posición de mercado en cualquier momento, y una
El cambio de señal primero cierra la posición anterior y luego abre una nueva en la dirección opuesta. Igual que el MetaTrader
código, la estrategia comienza con una señal corta virtual, lo que significa que la primera operación real ocurre solo después del promedio de cierre
se mueve por encima del promedio abierto.

## Lógica comercial
1. Suscríbase a la serie de velas definida por `CandleType` y procese solo velas terminadas para evitar decisiones prematuras.
2. Alimente dos promedios móviles simples con los precios de cierre y apertura de las velas. Ambos indicadores comparten el mismo `MovingPeriod` por lo que sus
Los valores se pueden comparar directamente.
3. Almacene los valores de media móvil recientes en buffers circulares. Los buffers permiten recuperar el valor de `MovingShift`
hace velas, emulando el parámetro de cambio MetaTrader sin llamar a métodos de indicador prohibidos.
4. Cuando el promedio de cierre desplazado esté por encima del promedio de apertura desplazado, configure la señal deseada en **comprar**. Cuando esté por debajo, configure el
señal deseada para **vender**. Si ambos promedios son iguales se conserva la señal anterior.
5. Si esta es la primera señal y no es alcista, manténgase plano. De lo contrario, si la señal deseada difiere de la última ejecutada
señal, cierre cualquier exposición existente y abra una nueva posición de mercado con `TradeVolume` lotes en la nueva dirección.
6. Actualice la señal almacenada para que las velas posteriores ignoren las instrucciones duplicadas mientras la dirección de la posición permanece sin cambios.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | marco de tiempo de 1 minuto | Plazo primario procesado por la estrategia. |
| `MovingPeriod` | `int` | `20` | Longitud de las medias móviles simples utilizadas en los precios de cierre y apertura. |
| `MovingShift` | `int` | `0` | Número de velas completadas en las que los valores de la media móvil se desplazan hacia atrás. |
| `TradeVolume` | `decimal` | `1` | Cantidad utilizada para cada orden de mercado. |

## Diferencias con el experto MetaTrader original
- Los asistentes de administración de dinero (stop loss, takeprofit, trailing stop) contenidos en el archivo de inclusión MQL no se transfieren. el
La versión StockSharp siempre intercambia un `TradeVolume` fijo y depende de controles de riesgo externos si es necesario.
- MetaTrader almacena pedidos individuales, mientras que StockSharp trabaja con posiciones netas. La conversión cierra la posición neta existente.
antes de abrir uno nuevo para que la exposición resultante coincida con el comportamiento de billete único de EA.
- El procesamiento de indicadores se maneja a través de la suscripción de velas de StockSharp API junto con los indicadores `SimpleMovingAverage` y
buffers internos en lugar de llamar a `iMA` directamente.

## Consejos de uso
- Ajuste `TradeVolume` al paso del lote del instrumento antes de comenzar la estrategia. El constructor también asigna el mismo valor a
`Strategy.Volume`, por lo que los métodos auxiliares emiten pedidos con el tamaño esperado.
- Aumente `MovingShift` si desea leer los promedios móviles de velas más antiguas, por ejemplo, para alinearse con la forma en que MetaTrader
Los gráficos de la plataforma cambiaron los indicadores.
- Agregue la estrategia a un gráfico para visualizar velas junto con los promedios móviles y las operaciones ejecutadas, lo que lo hace más fácil.
para confirmar que las reversiones ocurren exactamente cuando el promedio de cierre cruza el promedio de apertura.

## Indicadores
- Dos medias móviles simples (precio de cierre y precio de apertura) con longitudes idénticas y desplazamiento hacia atrás opcional.
