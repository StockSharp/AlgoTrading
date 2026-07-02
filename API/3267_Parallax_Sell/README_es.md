# Estrategia de Parallax Sell
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Parallax Sell es una estrategia martingala solo-corto convertida del asesor experto de MetaTrader `parallax_sell`. El robot original operaba cruces JPY (CAD/JPY y CHF/JPY) y se basa en una confluencia de filtros de Williams %R, MACD y oscilador estocástico para iniciar cortos en rallies de sobrecompra. Las salidas de posición dependen de signos de desvanecimiento del impulso proporcionados por Williams %R o un estocástico lento, mientras que un esquema de dimensionamiento de posición tipo martingala aumenta la exposición después de secuencias perdedoras.

## Lógica de entrada
- Trabajar en el marco temporal configurable (predeterminado: velas de 1 hora).
- Esperar un cierre de vela fresco.
- Requerir que Williams %R (período de retroceso de entrada 350) esté por encima del umbral de sobrecompra (predeterminado -10).
- Requerir que la línea principal del MACD (configuración 12/120/9) permanezca por encima de un umbral alcista (predeterminado 0.178) para confirmar el impulso alcista antes de desvanecerlo.
- Detectar un cruce descendente del %K estocástico rápido (longitud 10, ralentización 3) por debajo del nivel de disparo de entrada (predeterminado 90). Solo este evento de cruce puede producir un nuevo corto.
- Cada señal calificada envía una orden de venta de mercado adicional. Múltiples órdenes cortas pueden apilarse, siguiendo la lógica de volumen martingala.

## Lógica de salida
- Rastrear el beneficio flotante de todos los cortos abiertos en pips usando el tamaño de pip del instrumento.
- Si solo hay un corto abierto y el beneficio promedio supera el objetivo de operación única (predeterminado 10 pips) **y** Williams %R cae por debajo del umbral de salida (predeterminado -80), cerrar la posición.
- Si hay más de un corto abierto y el beneficio promedio de la cesta supera el objetivo de cesta (predeterminado 15 pips) **y** el %K estocástico lento (longitud 90, ralentización 1) cae por debajo del disparador de sobreventa (predeterminado 12), cerrar toda la cesta.
- Un take-profit de seguridad adicional cierra la cesta cuando la ganancia promedio alcanza la distancia de take-profit configurada (predeterminado 100 pips).

## Dimensionamiento de posición
- Comenzar con el volumen base (predeterminado 0.01 lotes).
- Después de un ciclo rentable (aumento del PnL realizado), restablecer el próximo volumen de orden al volumen base.
- Después de un ciclo perdedor (disminución del PnL realizado), multiplicar el próximo volumen de orden por el multiplicador martingala (predeterminado 1.6). Los volúmenes se alinean automáticamente al paso de volumen del instrumento.

## Gestión de riesgo
- La estrategia registra una orden protectora de take-profit usando la distancia de pip configurada. No se usa stop-loss fijo; las salidas son impulsadas por filtros de indicadores.
- La protección de inicio se activa una vez, según lo requerido por las directrices de conversión de StockSharp.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Marco temporal usado para los cálculos. | Velas de 1H |
| `EntryWilliamsLength` | Período de retroceso de Williams %R para entradas. | 350 |
| `ExitWilliamsLength` | Período de retroceso de Williams %R para salidas. | 350 |
| `EntryStochasticLength` / `Signal` / `Slowing` | Configuración del estocástico rápido para el cruce de entrada. | 10 / 1 / 3 |
| `ExitStochasticLength` / `Signal` / `Slowing` | Configuración del estocástico lento para confirmación de salida. | 90 / 7 / 1 |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | Parámetros del MACD. | 12 / 120 / 9 |
| `EntryWilliamsThreshold` | Valor mínimo de Williams %R requerido antes de ponerse corto. | -10 |
| `ExitWilliamsThreshold` | Nivel de Williams %R que confirma la salida para una sola operación. | -80 |
| `EntryStochasticTrigger` | Nivel que el estocástico rápido debe cruzar hacia abajo para disparar entradas. | 90 |
| `ExitStochasticTrigger` | Nivel al que el estocástico lento debe caer para cerrar cestas. | 12 |
| `MacdThreshold` | Valor mínimo de la línea principal del MACD. | 0.178 |
| `SingleTradeTargetPips` | Objetivo de beneficio (pips) cuando solo hay un corto activo. | 10 |
| `MultiTradeTargetPips` | Objetivo de beneficio (pips) cuando hay múltiples cortos activos. | 15 |
| `TakeProfitPips` | Distancia dura de take-profit (pips). | 100 |
| `InitialVolume` | Tamaño base de la orden. | 0.01 |
| `MartingaleMultiplier` | Multiplicador aplicado después de una pérdida cuando el martingala está habilitado. | 1.6 |
| `UseMartingale` | Habilitar o deshabilitar la escalada martingala. | true |

## Notas
- La estrategia solo opera posiciones cortas y asume convenciones de pip tipo Forex al medir beneficios.
- Los cálculos de beneficio promedio tratan cada entrada por igual, reflejando el bloque de MetaTrader que promediaba pips por operación.
- Ajuste los umbrales o deshabilite el martingala (`UseMartingale = false`) para reducir el riesgo en pares altamente volátiles.
