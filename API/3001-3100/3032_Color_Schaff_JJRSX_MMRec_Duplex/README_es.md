# Estrategia Color Schaff JJRSX MMRec Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port de StockSharp del asesor experto de MetaTrader `Exp_ColorSchaffJJRSXTrendCycle_MMRec_Duplex`. El robot original combina dos osciladores Schaff Trend Cycle impulsados por momentum JJRSX y un módulo MMRec (Recalculación de Gestión Monetaria) que reduce la exposición después de una secuencia de pérdidas. La conversión en C# preserva el diseño dual largo/corto y replica los controles de riesgo ajustables mientras reemplaza el indicador JJRSX no disponible con una aproximación robusta en la plataforma.

## Lógica de trading
- Se calculan dos osciladores independientes en marcos temporales seleccionados por el usuario: uno gobierna las entradas largas, el otro gobierna las entradas cortas. Cada oscilador usa líneas de momentum de estilo RSX rápidas y lentas, suavizadas y normalizadas con un pipeline de Schaff Trend Cycle para producir valores en el rango [-100, 100].
- Una posición larga se abre cuando el oscilador largo cruza hacia abajo a través de cero (`previous > 0` y `current <= 0`). El experto original marca estos eventos como reversiones de momentum alcistas. Las salidas largas se activan cuando el valor del indicador un bar antes es negativo.
- Una posición corta se abre cuando el oscilador corto cruza hacia arriba a través de cero (`previous < 0` y `current >= 0`). Las salidas cortas se activan cuando el valor del indicador un bar antes es positivo.
- El ajuste `SignalBar` reproduce el comportamiento de MetaTrader de evaluar señales en barras históricas. Por ejemplo, `SignalBar = 1` inspecciona la última vela completamente cerrada y la vela anterior a ella. La estrategia mantiene historiales de indicadores en movimiento para emular las llamadas `CopyBuffer` del código MQL.

## Gestión monetaria (MMRec)
- Se mantienen bloques de gestión monetaria separados para operaciones largas y cortas. El volumen base es igual a `Strategy.Volume * MM`, donde `MM` es el multiplicador normal configurable (`LongMm`/`ShortMm`).
- Después de cada operación cerrada, la estrategia registra si el resultado fue rentable o no (basándose en los precios de las velas de entrada/salida, idéntico a la lógica del EA que rastrea el historial a través de `HistorySelect`).
- Si las últimas `TotalTrigger` operaciones contienen al menos `LossTrigger` perdedores, la siguiente orden para ese lado cambia al multiplicador reducido (`SmallMm`). Cuando la condición de pérdida desaparece, el multiplicador base se restaura automáticamente.
- Los reversales de posición respetan las reglas de MMRec: el cambio de largo a corto (o viceversa) primero finaliza el resultado de la operación existente y actualiza los contadores de pérdidas antes de dimensionar la nueva orden.

## Aproximación del indicador
El robot original se basa en un indicador `ColorSchaffJJRSXTrendCycle` a medida construido sobre el oscilador JJRSX y las bibliotecas de suavizado Jurik. StockSharp no incluye esos componentes, por lo que la conversión implementa `ColorSchaffJjrsxTrendCycleIndicator`:
- Una aproximación RSI ligera (`SimpleRsi`) calcula la línea de base de momentum con suavizado exponencial idéntico al período de suavizado del EA.
- Las curvas RSI rápidas y lentas se restan para obtener una serie similar a MACD que luego se normaliza en una ventana cíclica y se suaviza doblemente con un factor configurable (predeterminado 0.5) para imitar el comportamiento de Schaff Trend Cycle.
- El indicador acepta las mismas fuentes de precio (cierre, apertura, máximo, mínimo, mediano, típico, ponderado, etc.) y retiene los parámetros de ciclo/longitud para que los flujos de trabajo de optimización permanezcan fieles a la estrategia de origen.

## Parámetros
| Grupo | Nombre | Descripción |
| --- | --- | --- |
| Long | `LongCandleType` | Tipo de vela o marco temporal usado para el indicador largo. |
| Long | `LongTotalTrigger` | Número de operaciones largas completadas inspeccionadas al evaluar el contador de pérdidas. |
| Long | `LongLossTrigger` | Número mínimo de pérdidas dentro de la ventana inspeccionada que activa el multiplicador reducido. |
| Long | `LongSmallMm` | Multiplicador de volumen reducido aplicado después de pérdidas repetidas. |
| Long | `LongMm` | Multiplicador de volumen largo predeterminado. |
| Long | `LongEnableOpen` | Habilita entradas largas. |
| Long | `LongEnableClose` | Habilita salidas largas. |
| Long | `LongFastLength` | Aproximación del período JJRSX rápido. |
| Long | `LongSlowLength` | Aproximación del período JJRSX lento. |
| Long | `LongSmooth` | Longitud de suavizado exponencial aplicada antes de la normalización de Schaff. |
| Long | `LongCycleLength` | Ventana de ciclo usada para la normalización mín/máx. |
| Long | `LongSignalBar` | Desplazamiento histórico usado al analizar señales largas. |
| Long | `LongAppliedPrice` | Fuente de precio usada por el indicador largo. |
| Short | `ShortCandleType` | Tipo de vela o marco temporal usado para el indicador corto. |
| Short | `ShortTotalTrigger` | Número de operaciones cortas completadas inspeccionadas al evaluar el contador de pérdidas. |
| Short | `ShortLossTrigger` | Número mínimo de pérdidas dentro de la ventana inspeccionada que activa el multiplicador reducido. |
| Short | `ShortSmallMm` | Multiplicador de volumen reducido aplicado después de pérdidas repetidas. |
| Short | `ShortMm` | Multiplicador de volumen corto predeterminado. |
| Short | `ShortEnableOpen` | Habilita entradas cortas. |
| Short | `ShortEnableClose` | Habilita salidas cortas. |
| Short | `ShortFastLength` | Aproximación del período JJRSX rápido para cortos. |
| Short | `ShortSlowLength` | Aproximación del período JJRSX lento para cortos. |
| Short | `ShortSmooth` | Longitud de suavizado exponencial aplicada antes de la normalización de Schaff para cortos. |
| Short | `ShortCycleLength` | Ventana de ciclo usada para la normalización mín/máx en el lado corto. |
| Short | `ShortSignalBar` | Desplazamiento histórico usado al analizar señales cortas. |
| Short | `ShortAppliedPrice` | Fuente de precio usada por el indicador corto. |

## Notas de implementación
- La estrategia usa las suscripciones de velas de alto nivel de StockSharp y evita el acceso directo a los buffers del indicador, siguiendo las directrices de conversión.
- Las protecciones (`StopLoss`/`TakeProfit`) de la versión MQL no se portan porque MetaTrader usa distancias basadas en puntos; los usuarios pueden adjuntar `StartProtection` o módulos de riesgo personalizados si es necesario.
- El historial de operaciones se evalúa usando los precios de cierre de las velas, lo que refleja la dependencia del EA en los registros de operaciones históricas mientras mantiene la lógica determinista dentro de StockSharp.
- El indicador personalizado expone `IsFormed` para que la estrategia solo reaccione una vez que se hayan acumulado suficientes datos, evitando señales prematuras durante el calentamiento.

## Aviso
Este port replica la estructura lógica de la estrategia de MetaTrader, pero el rendimiento puede diferir debido a los feeds de datos, las políticas de ejecución y la aproximación JJRSX. Valide siempre el comportamiento con datos de demostración antes de implementarlo en vivo.
