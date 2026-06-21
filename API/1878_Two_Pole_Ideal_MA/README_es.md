# Estrategia Two-Pole Ideal MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de cruce que aproxima el experto "2pb Ideal MA" comparando una EMA rápida con una TEMA lenta.

## Detalles

- **Criterios de entrada**: EMA rápida cruzando TEMA lenta.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Inversión en el cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `FastPeriod` = 10
  - `SlowPeriod` = 30
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, TEMA
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Swing (H4)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
