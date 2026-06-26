# Estrategia OHLC Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de momentum que usa el oscilador Stochastic clásico %K/%D en velas OHLC.
El algoritmo reacciona a cruces en zonas de sobrecompra/sobreventa y protege las operaciones abiertas con un trailing stop configurable medido en pasos de precio.

## Detalles

- **Idea principal**: explotar el cambio de momentum cuando el Stochastic %K cruza %D en niveles extremos.
- **Criterios de entrada**:
  - **Largo**:
    - %K cruza por encima de %D y al menos una de las líneas está por debajo del umbral `LevelDown`.
    - Si existe una posición corta, se cierra y se revierte a larga.
  - **Corto**:
    - %K cruza por debajo de %D y al menos una de las líneas está por encima del umbral `LevelUp`.
    - Si existe una posición larga, se cierra y se revierte a corta.
- **Criterios de salida**:
  - Se activa el trailing stop (basado en la distancia `TrailingStopSteps` y el requisito de mejora `TrailingStepSteps`).
  - Aparece la señal de entrada opuesta, desencadenando una reversión.
- **Lógica de trailing**:
  - La distancia y el paso se multiplican por el `PriceStep` del instrumento para convertir pips/pasos en precios absolutos.
  - El stop solo avanza después de que la operación se mueve más allá de `TrailingStopSteps + TrailingStepSteps` desde el precio de entrada.
  - Lógica de trailing separada para los lados largo y corto.
- **Indicadores**:
  - [StochasticOscillator](https://doc.stocksharp.com/html/T_StockSharp_Algo_Indicators_StochasticOscillator.htm) con `KPeriod`, `DPeriod` y `Slowing` ajustables.
- **Largo/Corto**: Ambos.
- **Stops**: Solo trailing stop (sin órdenes fijas de SL/TP).
- **Dimensionamiento de posición**: Usa el parámetro `Volume` de la estrategia; las reversiones envían `Volume + |Position|` para cambiar de dirección.
- **Parámetros predeterminados**:
  - `CandleType` = `TimeSpan.FromHours(12).TimeFrame()`
  - `KPeriod` = 5
  - `DPeriod` = 3
  - `Slowing` = 3
  - `LevelUp` = 70
  - `LevelDown` = 30
  - `TrailingStopSteps` = 5 (pasos de precio)
  - `TrailingStepSteps` = 2 (pasos de precio)
- **Visualización**:
  - Dibuja velas OHLC, indicador Stochastic y marcadores de operaciones cuando hay gráficos disponibles.

## Notas de uso

1. Configure el instrumento subyacente y el marco temporal antes de iniciar la estrategia.
2. Ajuste `TrailingStopSteps` según el tamaño del tick del instrumento para reflejar distancias reales en pips.
3. La estrategia llama a `StartProtection()` para que se puedan adjuntar reglas de riesgo adicionales externamente.
4. Funciona mejor en regímenes de tendencia donde las reversiones del Stochastic lideran el precio.
5. Para productos intradiarios, los marcos temporales más bajos pueden requerir reducir las distancias de trailing para evitar salidas prematuras.
