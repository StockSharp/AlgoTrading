# Estrategia de Cruces Zero-Lag TEMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de cruce de triple EMA sin rezago. Las posiciones usan máximos y mínimos recientes para los stops y objetivos basados en relación riesgo-recompensa.

## Detalles

- **Criterios de entrada**: TEMA rápida cruzando la TEMA lenta.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop en el extremo reciente o objetivo por ratio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Lookback` = 20
  - `FastPeriod` = 69
  - `SlowPeriod` = 130
  - `RiskReward` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: TEMA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
