# Estrategia Flash Calificador Minervini
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina el cruce de EMA, SuperTrend y RSI de momentum con el análisis de etapas de Minervini para calificar las operaciones.

## Detalles

- **Criterios de entrada**: EMA por encima de la línea de seguimiento, tendencia SuperTrend y RSI de momentum por encima del umbral con filtro de etapa Minervini
- **Largo/Corto**: Ambos
- **Criterios de salida**: seguimiento opuesto o giro de SuperTrend
- **Stops**: No
- **Valores predeterminados**:
  - `MomRsiLength` = 10
  - `MomRsiThreshold` = 60
  - `EmaLength` = 12
  - `EmaPercent` = 0.01
  - `SuperTrendPeriod` = 10
  - `SuperTrendMultiplier` = 3
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, SuperTrend, RSI
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
