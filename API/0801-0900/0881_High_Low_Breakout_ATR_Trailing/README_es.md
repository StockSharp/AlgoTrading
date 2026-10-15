# Estrategia de Ruptura de Máximos y Mínimos con Stop Trailing ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas del rango de los primeros 30 minutos de sesión. Una vez que el precio cruza el máximo o mínimo inicial, se abre una posición con un stop trailing basado en ATR. Todas las posiciones se cierran a una hora intradía especificada.

## Detalles
- **Criterios de entrada**:
  - **Largo**: El cierre cruza por encima del máximo de los primeros 30 minutos
  - **Corto**: El cierre cruza por debajo del mínimo de los primeros 30 minutos
- **Largo/Corto**: Configurable (`Direction`).
- **Criterios de salida**:
  - Stop trailing ATR o objetivo simétrico
  - Cerrar todas las posiciones en `ExitHour:ExitMinute`
- **Stops**: Sí, basado en ATR.
- **Valores predeterminados**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 3.5m
  - `RiskPerTrade` = 2m
  - `AccountSize` = 10000m
  - `SessionStartHour` = 9
  - `SessionStartMinute` = 15
  - `ExitHour` = 15
  - `ExitMinute` = 15
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Configurable
  - Indicadores: ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
