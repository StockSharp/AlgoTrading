# Rupturas y Retesteos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que entra en rupturas de máximos o mínimos recientes y en retesteos opcionales con gestión de stop trailing.

El enfoque rastrea el soporte y la resistencia definidos por los cierres más altos y más bajos en una ventana de retrospectiva. Las rupturas abren posiciones en la dirección de la ruptura o esperan un retesteo del nivel roto. Las salidas utilizan un stop-loss inicial que se convierte en un stop trailing una vez que el beneficio alcanza un umbral.

## Detalles

- **Criterios de entrada**: Ruptura por encima de la resistencia o por debajo del soporte, retesteo opcional.
- **Largo/Corto**: Configurable.
- **Criterios de salida**: Stop trailing o ruptura opuesta.
- **Stops**: Stop-loss inicial y stop trailing.
- **Valores predeterminados**:
  - `LookbackPeriod` = 20
  - `RetestBarsSinceBreakout` = 2
  - `RetestDetectionLimit` = 2
  - `ProfitThresholdPercent` = 5m
  - `TrailingStopGapPercent` = 1m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
