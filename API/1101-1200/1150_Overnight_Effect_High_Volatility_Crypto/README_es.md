# Estrategia de Efecto Nocturno de Alta Volatilidad en Cripto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que entra en una posición larga durante noches de alta volatilidad y cierra antes de la medianoche. La volatilidad se mide por la desviación estándar de los rendimientos logarítmicos durante un período configurable y se compara con la mediana de la volatilidad histórica.

## Detalles

- **Criterios de entrada**:
  - `currentHour == EntryHour && highVolatility` cuando `UseVolatilityFilter`
  - `currentHour == EntryHour` cuando el filtro está desactivado
- **Largo/Corto**: Largo
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `VolatilityPeriodDays` = 30
  - `MedianPeriodDays` = 208
  - `EntryHour` = 21
  - `ExitHour` = 23
  - `UseVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoría: Basada en tiempo
  - Dirección: Solo largos
  - Indicadores: StandardDeviation, Median
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
