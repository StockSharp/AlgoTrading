# Estrategia Supertrend nitin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia Supertrend de nitin en velas de 5 minutos.

## Detalles

- **Criterios de entrada**: Cambio de dirección hacia arriba.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cambio de dirección hacia abajo.
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
