# Estrategia de Rupturas con Filtro de Tiempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura que entra cuando el precio cruza por encima de máximos recientes o por debajo de mínimos recientes dentro de una sesión de operación especificada. Un filtro de media móvil opcional confirma la dirección. El stop-loss puede basarse en ATR, extremos de velas o puntos fijos con un objetivo de riesgo-recompensa configurable.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Cierre > máximo más alto en `Length` y dentro de la ventana de tiempo; opcionalmente Cierre > MA.
  - **Corto**: Cierre < mínimo más bajo en `Length` y dentro de la ventana de tiempo; opcionalmente Cierre < MA.
- **Largo/Corto**: Ambos
- **Stops**: ATR, basado en velas o puntos fijos con objetivo de riesgo-recompensa
- **Valores predeterminados**:
  - `Length` = 5
  - `MaLength` = 99
  - `UseMaFilter` = false
  - `UseTimeFilter` = true (14:30–15:00)
  - `SlType` = Atr
  - `SlLength` = 0
  - `AtrLength` = 14
  - `AtrMultiplier` = 0.5
  - `PointsStop` = 50
  - `RiskReward` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
