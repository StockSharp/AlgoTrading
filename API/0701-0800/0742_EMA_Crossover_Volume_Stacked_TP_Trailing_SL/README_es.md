# Estrategia de Cruce EMA con Volumen + TP Escalonado y SL Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera cruces de EMA filtrados por volumen. Establece dos objetivos de beneficio basados en ATR y aplica un trailing stop a la posición restante cuando el precio se mueve favorablemente.

## Detalles

- **Criterios de entrada**:
  - La EMA rápida cruza por encima/por debajo de la EMA lenta.
  - Volumen > volumen promedio * `VolumeMultiplier`.
- **Largo/Corto**: Largo y Corto.
- **Criterios de salida**:
  - Primer take profit en `TP1Multiplier * ATR` (33% de la posición).
  - Segundo take profit en `TP2Multiplier * ATR` (otro 33%).
  - El trailing stop se activa cuando el precio se mueve `TrailTriggerMultiplier * ATR` y sigue a `TrailOffsetMultiplier * ATR`.
- **Stops**: Solo trailing stop.
- **Valores predeterminados**:
  - `FastLength` = 21
  - `SlowLength` = 55
  - `VolumeMultiplier` = 1.2
  - `AtrLength` = 14
  - `Tp1Multiplier` = 1.5
  - `Tp2Multiplier` = 2.5
  - `TrailOffsetMultiplier` = 1.5
  - `TrailTriggerMultiplier` = 1.5
  - `CandleType` = 5m
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo/Corto
  - Indicadores: EMA, ATR, Volume
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
