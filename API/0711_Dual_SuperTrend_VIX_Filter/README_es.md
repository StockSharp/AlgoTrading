# Estrategia SuperTrend Dual con Filtro VIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina dos indicadores SuperTrend con un filtro de volatilidad basado en VIX. Se abre una posición larga cuando ambos SuperTrends son alcistas y el índice VIX está por encima de su media. Se abre una posición corta cuando ambos SuperTrends son bajistas y el VIX sube por encima de su media más un margen de desviación estándar. Las posiciones se cierran cuando cualquiera de los SuperTrends cambia de dirección.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Ambos SuperTrends indican tendencia alcista y el VIX está por encima de su media.
  - **Corto**: Ambos SuperTrends indican tendencia bajista y el VIX está por encima de su media y en aumento.
- **Criterios de salida**:
  - Señal opuesta de SuperTrend.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `StLength1` = 13
  - `StMultiplier1` = 3.5
  - `StLength2` = 8
  - `StMultiplier2` = 5
  - `UseVixFilter` = true
  - `VixLookback` = 252
  - `VixTrendPeriod` = 10
  - `StdDevMultiplier` = 1
  - `EnableLong` = true
  - `EnableShort` = true
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SuperTrend, SMA, StandardDeviation, EMA
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
