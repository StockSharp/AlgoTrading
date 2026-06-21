# Swing Multi-Indicador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de swing que combina Parabolic SAR, SuperTrend, ADX y confirmación por delta de volumen.

## Detalles

- **Criterios de entrada**: Todos los indicadores habilitados coinciden.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o alcanzar el stop-loss/take-profit.
- **Stops**: Niveles porcentuales opcionales.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(2)
  - `PsarStart` = 0.02m
  - `PsarIncrement` = 0.02m
  - `PsarMaximum` = 0.2m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `DeltaLength` = 14
  - `DeltaSmooth` = 3
  - `DeltaThreshold` = 0.5m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: PSAR, SuperTrend, ADX, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (2m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
