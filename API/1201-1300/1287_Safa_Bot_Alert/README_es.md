# Estrategia Safa Bot Alert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Safa Bot Alert utiliza una SMA corta con un filtro ADX para operar en cruces de precio. Entra largo cuando el precio cruza por encima de la SMA con una tendencia fuerte y entra corto en cruces por debajo. Un take profit fijo, stop loss y un stop trailing gestionan las posiciones, y todas las operaciones se cierran a una hora de sesión especificada.

## Detalles

- **Criterios de entrada**: El precio cruza la SMA y ADX > `AdxThreshold`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Take profit, stop loss, stop trailing o cierre de sesión.
- **Stops**: Fijo y Trailing.
- **Valores predeterminados**:
  - `SmaLength` = 3
  - `TakeProfitPoints` = 80m
  - `StopLossPoints` = 35m
  - `TrailPoints` = 15m
  - `AdxLength` = 15
  - `AdxThreshold` = 15m
  - `SessionCloseHour` = 16
  - `SessionCloseMinute` = 0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, ADX
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
