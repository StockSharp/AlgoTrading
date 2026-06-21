# Arbitraje Estadístico de Pares - Solo Lado Largo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia ejecuta un enfoque simple de trading de pares basado en el z-score del diferencial entre dos instrumentos. Abre una posición larga cuando el diferencial cae por debajo de un umbral definido por el usuario y cierra la posición cuando el diferencial cruza por encima de cero.

## Detalles

- **Criterios de entrada**: Z-score del diferencial por debajo del umbral.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Z-score del diferencial cruza por encima de cero.
- **Stops**: No.
- **Valores predeterminados**:
  - `ZScoreLength` = 20
  - `ExtremeLevel` = -1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Mean Reversion
  - Dirección: Largo
  - Indicadores: SMA, StandardDeviation
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
