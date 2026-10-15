# Plantilla de Estrategia TradingViewTo con Alertas Dinámicas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia plantilla que abre posiciones basadas en niveles de RSI y gestiona operaciones con stop loss y take profit porcentuales.

## Detalles
- **Criterios de entrada**:
  - **Largo**: RSI > `UpperLevel`
  - **Corto**: RSI < `LowerLevel`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Stop loss o take profit
- **Stops**: Stop loss y take profit porcentuales
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `UpperLevel` = 60
  - `LowerLevel` = 40
  - `StopLossPct` = 2m
  - `TakeProfitPct` = 4m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
