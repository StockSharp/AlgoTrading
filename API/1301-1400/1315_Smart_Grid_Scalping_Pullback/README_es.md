# Estrategia de Scalping de Pullback en Cuadrícula Inteligente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de scalping basada en cuadrícula que expande niveles de precio basados en ATR desde un precio base veinte barras atrás. Los pullbacks se filtran con RSI antes de las entradas. Las posiciones utilizan un objetivo de beneficio y un stop de seguimiento ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: close < basePrice - (LongLevel + 1) * ATR * GridFactor && range/low > NoTradeZone && RSI < MaxRsiLong && close > open
  - Corto: close > basePrice + (ShortLevel + 1) * ATR * GridFactor && range/high > NoTradeZone && RSI > MinRsiShort && close < open
- **Largo/Corto**: Ambos
- **Criterios de salida**: objetivo de beneficio o stop de seguimiento ATR
- **Stops**: Stop de seguimiento ATR
- **Valores predeterminados**:
  - `AtrLength` = 10
  - `GridFactor` = 0.35m
  - `ProfitTarget` = 0.004m
  - `NoTradeZone` = 0.003m
  - `ShortLevel` = 5
  - `LongLevel` = 5
  - `MinRsiShort` = 70
  - `MaxRsiLong` = 30
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Scalping
  - Dirección: Ambos
  - Indicadores: ATR, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
