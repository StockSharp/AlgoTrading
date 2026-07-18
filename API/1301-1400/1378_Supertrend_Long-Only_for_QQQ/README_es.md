# Estrategia Supertrend Solo Largos para QQQ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia solo largos basada en el indicador Supertrend y un filtro de rango de fechas.

## Detalles

- **Criterios de entrada**: Precio cruzando por encima del Supertrend.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Precio cruzando por debajo del Supertrend.
- **Stops**: No.
- **Valores predeterminados**:
  - `AtrPeriod` = 32
  - `Multiplier` = 4.35m
  - `StartDate` = 1995-01-01
  - `EndDate` = 2050-01-01
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: ATR, Supertrend
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
