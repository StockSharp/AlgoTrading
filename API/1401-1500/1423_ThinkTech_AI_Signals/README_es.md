# Estrategia de Señales AI de ThinkTech
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas de la primera vela de 15 minutos de la sesión. Utiliza niveles de stop loss y take profit basados en ATR y puede aplicar filtros opcionales de tendencia y RSI.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio rompe por encima del máximo de la primera vela con los filtros de tendencia y RSI satisfechos.
  - **Corto**: El precio rompe por debajo del mínimo de la primera vela con los filtros de tendencia y RSI satisfechos.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Alcanzar el nivel de take profit o stop loss.
- **Stops**: Sí, basados en ATR.
- **Valores predeterminados**:
  - `RiskRewardRatio` = 2
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
