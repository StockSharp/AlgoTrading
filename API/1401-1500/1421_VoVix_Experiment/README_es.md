# Estrategia del Experimento VoVix
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia analiza la razón entre el ATR rápido y el ATR lento. Cuando el z-score de esta razón se dispara y alcanza un máximo local, entra en la dirección de la vela. Las posiciones se cierran cuando el z-score cae por debajo del umbral de salida.

## Detalles

- **Criterios de entrada**: Z-score del VoVix por encima de `EntryZ` y en máximo local
- **Largo/Corto**: Ambos
- **Criterios de salida**: Z-score del VoVix por debajo de `ExitZ`
- **Stops**: No
- **Valores predeterminados**:
  - `FastAtrLength` = 13
  - `SlowAtrLength` = 26
  - `ZScoreWindow` = 81
  - `EntryZ` = 1.0
  - `ExitZ` = 1.4
  - `LocalMaxWindow` = 6
  - `SuperZ` = 2.0
  - `MinVolume` = 1
  - `MaxVolume` = 2
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Ambos
  - Indicadores: ATR, Highest, SMA, StdDev
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
