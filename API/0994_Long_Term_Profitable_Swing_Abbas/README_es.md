# Estrategia de Swing Rentable a Largo Plazo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando la EMA rápida cruza hacia arriba la EMA lenta y el RSI está por encima de un umbral especificado. Las salidas ocurren cuando el precio alcanza los niveles de stop loss o take profit basados en ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: la EMA rápida cruza hacia arriba la EMA lenta y RSI > umbral.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - El precio alcanza el stop loss o take profit basado en ATR.
- **Stops**: Múltiplos ATR para stop loss y take profit.
- **Valores predeterminados**:
  - `FastEmaLength` = 16
  - `SlowEmaLength` = 30
  - `RsiLength` = 9
  - `AtrLength` = 21
  - `RsiThreshold` = 50
  - `AtrStopMult` = 8
  - `AtrTpMult` = 11
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo
  - Indicadores: EMA, RSI, ATR
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
