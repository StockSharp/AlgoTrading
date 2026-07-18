# Estrategia de Divergencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en la divergencia entre precio y RSI con detección simple de pivotes.

La Estrategia de Divergencia utiliza máximos y mínimos de pivote en el precio y el RSI para detectar divergencias alcistas y bajistas. Cuando el precio hace un nuevo máximo pero el RSI no lo confirma, la estrategia vende. Por el contrario, cuando el precio hace un nuevo mínimo mientras el RSI sube, compra.

## Detalles

- **Criterios de entrada**: Divergencias entre precio y RSI.
- **Largo/Corto**: Ambas direcciones (configurable).
- **Criterios de salida**: Señal opuesta del RSI u órdenes de protección.
- **Stops**: Sí (stop loss y take profit).
- **Valores predeterminados**:
  - `TradeDirection` = Both
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `RiskReward` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
