# Estrategia Charles SMA con Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera usando el cruce de dos Medias Móviles Simples y una gestión opcional de trailing stop. Cuando la SMA rápida cruza por encima de la SMA lenta se abre una posición larga. Se abre una posición corta cuando la SMA rápida cruza por debajo de la SMA lenta. La estrategia soporta stop-loss fijo, take-profit y un trailing stop que se activa después de un umbral de beneficio predefinido.

## Detalles

- **Criterios de entrada**:
  - SMA rápida cruza por encima de la SMA lenta → abrir largo.
  - SMA rápida cruza por debajo de la SMA lenta → abrir corto.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Cruce inverso.
  - Stop-loss o take-profit alcanzado.
  - Trailing stop activado cuando el beneficio alcanza `TrailStart` y sigue con `TrailingAmount`.
- **Stops**:
  - `StopLoss` define un stop protector fijo en unidades de precio.
  - `TakeProfit` define un objetivo de beneficio fijo.
  - `TrailStart` y `TrailingAmount` controlan el trailing stop.
- **Valores predeterminados**:
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `StopLoss` = 0
  - `TakeProfit` = 25
  - `TrailStart` = 25
  - `TrailingAmount` = 5
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: SMA
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
