# Estrategia Z-Score Normalized VIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que promedia los z-scores de varios índices VIX y entra en largo cuando el valor combinado cae por debajo de un umbral negativo.

El algoritmo calcula el z-score para VIX, VIX3M, VIX9D y VVIX. Los z-scores seleccionados se promedian para formar un único indicador que representa el sentimiento general de volatilidad.

## Detalles

- **Criterios de entrada**: Z-score combinado por debajo de `-Threshold`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Z-score combinado sube por encima de `-Threshold`.
- **Stops**: No.
- **Valores predeterminados**:
  - `ZScoreLength` = 6
  - `Threshold` = 1
  - `UseVix` = true
  - `UseVix3m` = true
  - `UseVix9d` = true
  - `UseVvix` = true
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Largo
  - Indicadores: Z-Score
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
