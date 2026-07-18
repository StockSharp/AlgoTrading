# Estrategia Supertrend (5m)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia Supertrend en velas de 5 minutos.

## Detalles

- **Criterios de entrada**: Precio cruzando por encima del Supertrend.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Precio cruzando por debajo del Supertrend.
- **Stops**: No.
- **Valores predeterminados**:
  - `AtrPeriod` = 10
  - `Multiplier` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: ATR, Supertrend
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
