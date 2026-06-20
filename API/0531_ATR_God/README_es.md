# ATR GOD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina una entrada de Supertrend con stop loss y take profit basados en ATR.

## Detalles

- **Criterios de entrada**: Cambio de dirección del Supertrend.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop de ATR o señal opuesta.
- **Stops**: Basado en ATR.
- **Valores predeterminados**:
  - `Period` = 10
  - `Multiplier` = 3m
  - `RiskMultiplier` = 4.5m
  - `RewardRiskRatio` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, Supertrend
  - Stops: ATR
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

