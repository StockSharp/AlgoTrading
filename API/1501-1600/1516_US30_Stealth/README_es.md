# Estrategia US30 Stealth
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de price action para US30 que utiliza la pendiente de la media móvil, patrones envolventes, volumen y filtro de sesión.
El tamaño de posición se calcula a partir del riesgo por operación, con stop-loss y take-profit basados en el rango de la vela.

## Detalles

- **Criterios de entrada**: Dirección de tendencia, tres máximos decrecientes o mínimos crecientes, patrón envolvente, filtro de volumen y tiempo.
- **Largo/Corto**: Ambos
- **Criterios de salida**: Take-profit o stop-loss
- **Stops**: Fijo
- **Valores predeterminados**:
  - `MaLen` = 50
  - `VolMaLen` = 20
  - `HlLookback` = 5
  - `RrRatio` = 2.2
  - `MaxCandleSize` = 30
  - `PipValue` = 1
  - `RiskAmount` = 50
  - `LargeCandleThreshold` = 25
  - `MaSlopeLen` = 3
  - `MinSlope` = 0.1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Price action
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
