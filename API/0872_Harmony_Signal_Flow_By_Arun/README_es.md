# Estrategia Harmony Signal Flow By Arun
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Harmony Signal Flow By Arun utiliza un RSI de período corto para capturar reversiones con niveles fijos de stop-loss y objetivo. La estrategia va largo cuando el RSI cruza por encima del umbral inferior y corto cuando cruza por debajo del umbral superior. Las posiciones se cierran por stop, objetivo o a las 15:25 de cada día.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: El RSI cruza por encima de `LowerThreshold`.
  - **Corto**: El RSI cruza por debajo de `UpperThreshold`.
- **Criterios de salida**: Stop-loss u objetivo alcanzado, o cierre a las 15:25.
- **Stops**: Stop-loss y objetivo fijos.
- **Valores predeterminados**:
  - `RsiPeriod` = 5
  - `LowerThreshold` = 30
  - `UpperThreshold` = 70
  - `BuyStopLoss` = 100
  - `BuyTarget` = 150
  - `SellStopLoss` = 100
  - `SellTarget` = 150
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo y Corto
  - Indicadores: RSI
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
