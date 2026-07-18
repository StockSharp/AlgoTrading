# Estrategia de Caza de Swings Multi-Confluencia V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Caza de Swings Multi-Confluencia V1 utiliza un sistema de puntuación que combina RSI, MACD y acción del precio para identificar mínimos y máximos de swing. Una operación larga se abre cuando las señales alcistas alcanzan la puntuación mínima de entrada y se cierra cuando las señales bajistas alcanzan la puntuación de salida.

## Detalles

- **Criterios de entrada**: Puntuación de entrada ≥ `MinEntryScore` a partir de señales RSI/MACD y estructura alcista de velas.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Puntuación de salida ≥ `MinExitScore` a partir de señales RSI/MACD y estructura bajista de velas.
- **Stops**: No.
- **Valores predeterminados**:
  - `MacdFast` = 3
  - `MacdSlow` = 10
  - `MacdSignal` = 3
  - `RsiLength` = 21
  - `MinEntryScore` = 13
  - `MinExitScore` = 13
  - `MinLowerWickPercent` = 50
  - `RsiOversold` = 30
  - `RsiExtremeOversold` = 25
  - `RsiOverbought` = 70
  - `RsiExtremeOverbought` = 75
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Solo largos
  - Indicadores: RSI, MACD
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
