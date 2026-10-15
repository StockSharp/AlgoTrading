# Estrategia del Sistema Turtle y Tendencia de Crunchster
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina un filtro de tendencia EMA rápida/lenta con entradas por ruptura del canal Donchian y gestión de stops basada en ATR. Un canal Donchian en trailing sale de las posiciones cuando el momentum se revierte.

## Detalles

- **Criterios de entrada**: Cruce diferencial de EMA o ruptura de canal Donchian.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Canal trailing o stop ATR.
- **Stops**: Sí, basados en ATR.
- **Valores predeterminados**:
  - `CandleType` = 1 hora
  - `FastEmaPeriod` = 10
  - `BreakoutPeriod` = 20
  - `TrailPeriod` = 1000
  - `StopAtrMultiple` = 20
  - `OrderPercent` = 10
  - `TrendEnabled` = true
  - `BreakoutEnabled` = false
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo y Corto
  - Indicadores: EMA, Donchian, ATR
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
