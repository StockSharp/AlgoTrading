# Estrategia Supertrend Multi-Paso Estratégica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza dos cálculos de Supertrend para detectar entradas y salidas con tomas de beneficio de múltiples pasos configurables.

## Detalles

- **Criterios de entrada**: Señales basadas en las direcciones de dos Supertrend.
- **Largo/Corto**: Configurable.
- **Criterios de salida**: Supertrend opuesto o niveles de toma de beneficio.
- **Stops**: Pasos de toma de beneficio.
- **Valores predeterminados**:
  - `UseTakeProfit` = true
  - `TakeProfitPercent1` = 6.0
  - `TakeProfitPercent2` = 12.0
  - `TakeProfitPercent3` = 18.0
  - `TakeProfitPercent4` = 50.0
  - `TakeProfitAmount1` = 12
  - `TakeProfitAmount2` = 8
  - `TakeProfitAmount3` = 4
  - `TakeProfitAmount4` = 0
  - `NumberOfSteps` = 3
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 5
  - `Factor2` = 4.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Configurable
  - Indicadores: ATR, Supertrend
  - Stops: Toma de beneficio
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
