# Estrategia NY ORB CP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura del rango de apertura de NY con confirmación de retest. Opera rupturas del rango de 9:30-9:45 de NY cuando el precio realiza un retest y reanuda la dirección del rompimiento.

## Detalles

- **Criterios de entrada**:
  - Largo: El precio retesta el máximo de NY tras el rompimiento con confirmación de tendencia y volumen.
  - Corto: El precio retesta el mínimo de NY tras el rompimiento bajista con confirmación de tendencia y volumen.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Objetivo de beneficio en 0.33 del rango * `RiskReward`.
  - Stop loss en 0.33 del rango.
- **Stops**: Sí, dinámicos.
- **Valores predeterminados**:
  - `MinRangePoints` = 60
  - `RiskReward` = 3
  - `MaxTradesPerSession` = 3
  - `MaxDailyLoss` = -1000
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: EMA, VWAP, SMA
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
