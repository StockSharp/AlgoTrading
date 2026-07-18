# Estrategia de Pullback Fibonacci Adaptativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia promedia tres líneas SuperTrend construidas con multiplicadores de Fibonacci (0.618, 1.618, 2.618) y suaviza el resultado con una EMA. Las operaciones siguen pullbacks hacia esta tendencia adaptativa, mientras que una línea media basada en AMA y un filtro RSI opcional confirman la dirección.

## Detalles

- **Criterios de entrada**:
  - Mínimo por debajo del SuperTrend promediado y cierre por encima de su valor suavizado.
  - El cierre previo relativo a la línea media AMA define el pullback.
  - **Largo**: cierre por encima de la línea media y RSI > umbral.
  - **Corto**: cierre por debajo de la línea media y RSI < umbral.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Cierre que cruza el SuperTrend suavizado en dirección opuesta.
- **Stops**: Stop loss y take profit porcentuales mediante `StartProtection`.
- **Valores predeterminados**:
  - `AtrPeriod` = 8
  - `SmoothLength` = 21
  - `AmaLength` = 55
  - `RsiLength` = 7
  - `RsiBuy` = 70
  - `RsiSell` = 30
  - `TakeProfitPercent` = 5
  - `StopLossPercent` = 0.75
- **Filtros**:
  - Categoría: Pullback de tendencia
  - Dirección: Ambos
  - Indicadores: SuperTrend, EMA, AMA, RSI
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
