# Estrategia Exp XPeriodCandle X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Exp XPeriodCandle X2 recrea el experto original de MetaTrader usando la API de alto nivel de StockSharp. La estrategia construye velas sintéticas en dos marcos temporales suavizando cada barra y comparando la apertura retrasada de una ventana de retroceso configurable con el último cierre suavizado. El color de la vela del marco temporal superior define el sesgo de tendencia, mientras que el marco temporal de trabajo espera transiciones de color para activar entradas y salidas. Las protecciones opcionales de stop-loss y take-profit replican las entradas de gestión monetaria del código fuente.

## Cómo funciona
- **Detección de tendencia** – la suscripción del marco temporal superior suaviza los precios de apertura y cierre con la media móvil seleccionada. Cada vela completada compara su cierre suavizado con la apertura suavizada retrasada de `TrendPeriod` barras atrás. Un cierre por encima de la apertura retrasada produce un color alcista (0), mientras que un cierre por debajo produce un color bajista (2). El color almacenado en `TrendSignalBar` determina si la tendencia global es larga (`+1`), corta (`-1`) o neutral.
- **Lógica de entrada** – el marco temporal de trabajo aplica el mismo suavizado. Para cada vela terminada la estrategia almacena los colores actual y anterior referenciados por `EntrySignalBar`. Una configuración corta aparece cuando la tendencia del marco temporal superior es bajista, el color actual es 0 y el color anterior es 2, reflejando el flip de señal XPeriodCandle original. Una configuración larga requiere que la tendencia sea alcista, el color actual sea 2 y el color anterior sea 0.
- **Gestión de posición** – los interruptores configurables cierran posiciones en flips de tendencia (`CloseLongOnTrendFlip`, `CloseShortOnTrendFlip`) y en reversiones de nivel de entrada (`CloseLongOnEntrySignal`, `CloseShortOnEntrySignal`). Los nuevos trades dimensionan `Volume + |Position|`, por lo que una señal opuesta tanto sale como revierte como el experto MQL.
- **Controles de riesgo** – las distancias opcionales de stop-loss y take-profit se expresan en pasos de precio (`StopLossTicks`, `TakeProfitTicks`). Se activan solo cuando el booleano correspondiente está habilitado.
- **Métodos de suavizado** – se usan las medias móviles de StockSharp en lugar de la biblioteca SmoothAlgorithms original. Los modos disponibles son Simple, Exponencial, Suavizado (SMMA), Ponderado, Hull, Kaufman Adaptive y Jurik. Los parámetros `TrendPhase` y `EntryPhase` afectan solo el suavizado Jurik y están limitados al rango ±100.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `TrendCandleType` | Tipo de vela de marco temporal superior usada para el filtro de tendencia. |
| `EntryCandleType` | Tipo de vela de marco temporal de trabajo usada para entradas. |
| `TrendPeriod` | Número de velas suavizadas que definen la apertura retrasada en el marco temporal de tendencia. |
| `EntryPeriod` | Número de velas suavizadas que definen la apertura retrasada en el marco temporal de entrada. |
| `TrendLength` | Longitud de suavizado para velas sintéticas de marco temporal superior. |
| `EntryLength` | Longitud de suavizado para velas sintéticas de marco temporal de trabajo. |
| `TrendPhase` | Parámetro de fase Jurik para el marco temporal de tendencia (ignorado por otros tipos de suavizado). |
| `EntryPhase` | Parámetro de fase Jurik para el marco temporal de entrada (ignorado por otros tipos de suavizado). |
| `TrendSignalBar` | Desplazamiento usado para leer el color de la vela de tendencia (`1` coincide con la barra más recientemente cerrada). |
| `EntrySignalBar` | Desplazamiento usado para leer colores de entrada (`1` referencia la última barra cerrada, `2` la anterior). |
| `TrendSmoothing` | Tipo de media móvil aplicada al suavizado de marco temporal superior. |
| `EntrySmoothing` | Tipo de media móvil aplicada al suavizado de marco temporal de trabajo. |
| `EnableLongEntries` | Permitir posiciones largas cuando aparecen condiciones alcistas. |
| `EnableShortEntries` | Permitir posiciones cortas cuando aparecen condiciones bajistas. |
| `CloseLongOnTrendFlip` | Cerrar posiciones largas cuando la tendencia del marco temporal superior se vuelve bajista. |
| `CloseShortOnTrendFlip` | Cerrar posiciones cortas cuando la tendencia del marco temporal superior se vuelve alcista. |
| `CloseLongOnEntrySignal` | Cerrar posiciones largas cuando el marco temporal de entrada imprime un color bajista. |
| `CloseShortOnEntrySignal` | Cerrar posiciones cortas cuando el marco temporal de entrada imprime un color alcista. |
| `UseStopLoss` | Habilitar protección de stop-loss medida en pasos de precio. |
| `StopLossTicks` | Distancia de stop-loss en pasos de precio. |
| `UseTakeProfit` | Habilitar protección de take-profit medida en pasos de precio. |
| `TakeProfitTicks` | Distancia de take-profit en pasos de precio. |

## Notas
- La lógica de apertura retrasada almacena la apertura suavizada más antigua dentro del período configurado, coincidiendo con el búfer circular del indicador original.
- Cuando `TrendCandleType` y `EntryCandleType` son iguales, solo se crea una suscripción de vela pero la lógica de doble color sigue funcionando.
- Asegurarse de que `Volume` esté configurado apropiadamente; los trades de reversión incluyen automáticamente la posición absoluta actual para replicar el comportamiento de manejo de lotes de MetaTrader.
