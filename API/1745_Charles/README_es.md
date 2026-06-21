# Estrategia Charles EMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia emula el asesor experto Charles combinando medias móviles exponenciales (EMA) con un filtro RSI y un trailing stop. Opera en ambas direcciones y protege las posiciones dinámicamente.

El sistema monitorea una EMA rápida y una lenta en el marco temporal seleccionado. Cuando la EMA rápida cruza por encima de la EMA lenta y el RSI supera 55, la estrategia entra en una posición larga. Por el contrario, cuando la EMA rápida cruza por debajo de la EMA lenta y el RSI cae por debajo de 45, entra en una posición corta. Tras el ingreso, un trailing stop sigue al precio para consolidar ganancias mientras que un take profit fijo y stop loss se gestionan mediante la protección de posición integrada.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `EMA rápida` cruza por encima de `EMA lenta` y `RSI > 55`.
  - **Corto**: `EMA rápida` cruza por debajo de `EMA lenta` y `RSI < 45`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Trailing stop.
  - Stop loss o take profit.
- **Stops**: Usa la protección integrada con trailing.
- **Valores predeterminados**:
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `RsiPeriod` = 14
  - `TakeProfit` = 0.02
  - `StopLoss` = 0.008
  - `TrailStart` = 0.006
  - `TrailOffset` = 0.003
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: 1 hora por defecto
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
