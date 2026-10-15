# Estrategia de Ruptura y Retest de la Primera Vela NY
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera Rupturas de la primera vela de la sesión de Nueva York con confirmación de retest. Utiliza ATR para colocación de stops y objetivos de rentabilidad-riesgo con filtro de tendencia EMA opcional y trailing stop.

## Detalles

- **Criterios de entrada**: Ruptura del máximo o mínimo de la primera vela de sesión seguido de un retest dentro de `RetestThreshold` ATR.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop basado en ATR y objetivo `RewardRiskRatio`. Trailing stop opcional.
- **Stops**: `AtrMultiplier` * ATR.
- **Valores predeterminados**:
  - `NyStartHour` = 9
  - `NyStartMinute` = 30
  - `SessionLength` = 4
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.2
  - `RewardRiskRatio` = 1.5
  - `MinBreakSize` = 0.15
  - `RetestThreshold` = 0.25
  - `UseEmaFilter` = true
  - `EmaLength` = 13
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: ATR, EMA
  - Stops: ATR
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
