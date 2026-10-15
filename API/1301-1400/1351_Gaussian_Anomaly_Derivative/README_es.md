# Estrategia de Derivada de Anomalía Gaussiana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza una media móvil de la anomalía de precio `1 - (high + low) / (2 * close)` y su derivada suavizada.
Opera largo cuando la derivada supera su umbral positivo y corto cuando cae por debajo del umbral negativo.

## Detalles

- **Criterios de entrada**: la anomalía o su derivada cruza el umbral
- **Largo/Corto**: Configurable
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `UseSma` = true
  - `MaPeriod` = 3
  - `DerivativeMaPeriod` = 2
  - `ThresholdCoeff` = 1.0
  - `DerivativeThresholdCoeff` = 1.0
  - `StartBarCount` = 600
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA, EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
