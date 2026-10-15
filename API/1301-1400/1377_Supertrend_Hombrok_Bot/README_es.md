# Estrategia Supertrend Hombrok Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia Supertrend con filtros de volumen, tamaño del cuerpo y RSI, con stop y toma de ganancias basados en ATR.

## Detalles
- **Criterios de entrada**: Tendencia alcista con filtros de volumen y cuerpo y RSI por debajo de sobrecompra para largos; tendencia bajista con filtros y RSI por encima de sobreventa para cortos
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop loss o toma de ganancias basados en ATR
- **Stops**: Stop fijo y toma de ganancias desde ATR
- **Valores predeterminados**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70m
  - `RsiOversold` = 30m
  - `VolumeMultiplier` = 1.2m
  - `BodyPctOfAtr` = 0.3m
  - `RiskRewardRatio` = 2m
  - `CapitalPerTrade` = 10m
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Supertrend, RSI, ATR, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
