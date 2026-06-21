# Estrategia de Canal Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas del Canal Keltner y cruces de tendencia de EMA.

## Detalles

- **Criterios de entrada**:
  - Largo: el precio cruza por debajo de la banda inferior del Keltner o la EMA9 cruza por encima de la EMA21 mientras el precio está por encima de la EMA50.
  - Corto: el precio cruza por encima de la banda superior del Keltner o la EMA9 cruza por debajo de la EMA21 mientras el precio está por debajo de la EMA50.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - El precio cruza la banda media en la dirección opuesta o las EMA se cruzan de vuelta.
  - Stop loss a 1.5 ATR.
  - Take profit a 3 ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Length` = 20
  - `Multiplier` = 1.5
  - `AtrMultiplier` = 1.5
  - `FastEmaPeriod` = 9
  - `SlowEmaPeriod` = 21
  - `TrendEmaPeriod` = 50
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Canal
  - Dirección: Ambos
  - Indicadores: EMA, ATR, Keltner
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
