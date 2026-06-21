# Estrategia de Reversión a la Media ATR Vender el Rebote
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia solo corto que vende cuando el precio sube por encima de un umbral ATR suavizado y cubre en una caída por debajo del mínimo anterior. Un filtro EMA opcional limita las operaciones a tendencias bajistas.

## Detalles

- **Criterios de entrada**: Cierre por encima del suavizado (close + ATR * multiplicador)
- **Largo/Corto**: Corto
- **Criterios de salida**: Cierre por debajo del mínimo anterior
- **Stops**: No
- **Valores predeterminados**:
  - `AtrPeriod` = 20
  - `AtrMultiplier` = 1.0
  - `SmoothPeriod` = 10
  - `EmaPeriod` = 200
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Corto
  - Indicadores: ATR, SMA, EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
