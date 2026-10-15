# Estrategia de Mapa de Calor Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Stochastic Heat Map promedia un conjunto de osciladores Stochastic con períodos crecientes.
La lectura combinada se suaviza nuevamente para formar una línea rápida y una lenta.
Las operaciones van largas cuando la línea rápida cruza por encima de la lenta y cortas en el cruce opuesto.

## Detalles

- **Criterios de entrada**: cruce de línea rápida/lenta
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `Increment` = 10
  - `SmoothFast` = 2
  - `SmoothSlow` = 21
  - `PlotNumber` = 28
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Stochastic
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
