# MACD Estrategia PSAR fija
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una adaptación de C# del asesor experto MetaTrader **EA_MACD_FixedPSAR**. Intercambia cambios de tendencia combinando un filtro cruzado MACD con una verificación de tendencia EMA. La gestión de riesgos refleja la implementación original y admite tanto un trailing stop de distancia fija como un modo de seguimiento estilo Parabolic SAR. Todas las distancias se configuran en pips y se convierten internamente a unidades de precio según el tamaño del tick del instrumento.

## Indicadores
- `MovingAverageConvergenceDivergenceSignal` (12, 26, 9) entrega MACD y líneas de señal.
- `ExponentialMovingAverage` (predeterminado 26) confirma la dirección de la tendencia a corto plazo.

## Lógica de trading
1. **Condiciones de entrada**
   - **Largo**: MACD cruza por encima de su línea de señal mientras permanece por debajo de cero, el valor absoluto de MACD excede el *MACD nivel de apertura* y el EMA está aumentando en comparación con la vela anterior.
   - **Corto**: MACD cruza por debajo de su línea de señal mientras permanece por encima de cero, el valor absoluto de MACD excede el *MACD nivel de apertura* y el EMA está cayendo en comparación con la vela anterior.
2. **Condiciones de salida**
   - MACD reversión que excede el *MACD Nivel de Cierre* en la dirección opuesta.
   - Niveles configurables de toma de ganancias y límite de pérdidas, ambos medidos en pips.
   - Comportamiento de trailing stop opcional:
     - **Fijo**: mantiene una distancia constante desde el último cierre.
     - **Fijo PSAR**: emula el ajuste incremental Parabolic SAR utilizado por la versión MQL.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| `Volume` | Volumen de negociación utilizado para órdenes de mercado. |
| `TakeProfitPips` | Distancia de toma de ganancias en pips. |
| `StopLossPips` | Distancia de stop-loss en pips. |
| `TrailMode` | Lógica de trailing stop (`None`, `Fixed`, `FixedPsar`). |
| `TrailingStopPips` | Distancia para el modo de seguimiento fijo. |
| `PsarStep` | Factor de aceleración inicial para el modo de seguimiento PSAR. |
| `PsarMaximum` | Factor de aceleración máximo para el modo de seguimiento PSAR. |
| `MacdOpenLevelPips` | Magnitud mínima de MACD (en pips) necesaria para abrir una posición. |
| `MacdCloseLevelPips` | Magnitud mínima MACD (en pips) necesaria para cerrar una posición. |
| `TrendPeriod` | EMA período utilizado para la confirmación de tendencias. |
| `CandleType` | Tipo de serie de velas para cálculos de indicadores. |

## Notas
- Todos los umbrales se almacenan en pips y se traducen a unidades de precio utilizando el tamaño del tick del instrumento (con cinco o tres correcciones decimales que emulan el ajuste MetaTrader.
- La lógica de trailing stop se actualiza solo en velas completamente formadas para evitar salidas prematuras.
- La estrategia dibuja velas, ambos indicadores y marcas comerciales en el área del gráfico predeterminado cuando están disponibles.
