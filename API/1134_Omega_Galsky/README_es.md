# Estrategia Omega Galsky
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de cruce de EMA con lógica de stop de equilibrio.

## Detalles

- **Criterios de entrada**: La EMA rápida cruza la EMA lenta con confirmación de precio de EMA89.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss, take profit o señal opuesta.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Ema8Period` = 8
  - `Ema21Period` = 21
  - `Ema89Period` = 89
  - `FixedRiskReward` = 1.0m
  - `SlPercentage` = 0.001m
  - `TpPercentage` = 0.0025m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
