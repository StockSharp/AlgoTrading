# Estrategia de Tendencia Chande Kroll
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza el stop Chande Kroll con un filtro de tendencia SMA. Se abre una posición larga cuando el cierre cruza por encima del stop inferior y está por encima de la SMA. La posición se cierra cuando el cierre cae por debajo del stop superior. El tamaño de la posición se basa en el cierre mínimo en 1560 barras y el multiplicador de riesgo.

## Detalles

- **Criterios de entrada**:
  - Largo: `previous close <= previous low stop && Close > low stop && Close > SMA`
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - Largo: `Close < high stop`
- **Stops**: Stop Chande Kroll (extremos Donchian ± ATR)
- **Valores predeterminados**:
  - `CalcMode` = CalcMode.Exponential
  - `RiskMultiplier` = 5m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `StopLength` = 21
  - `SmaLength` = 21
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: ATR, Donchian, SMA, Lowest
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
