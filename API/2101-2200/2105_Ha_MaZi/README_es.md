# Estrategia Ha MaZi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina velas Heikin Ashi, un filtro EMA y confirmación de pivote ZigZag. Se abre una operación larga cuando se forma una vela Heikin Ashi alcista en un nuevo mínimo de ZigZag por encima de la EMA. Las posiciones cortas aparecen en una vela bajista en un nuevo máximo de ZigZag por debajo de la EMA. Las posiciones se cierran por stop loss fijo o take profit.

## Detalles
- **Criterios de entrada**: Pivote ZigZag con dirección Heikin Ashi y filtro EMA.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss o take profit.
- **Stops**: Stop y objetivo fijos.
- **Valores predeterminados**:
  - `MaPeriod` = 40
  - `ZigzagLength` = 13
  - `StopLoss` = 70
  - `TakeProfit` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Heikin Ashi, EMA, ZigZag
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
